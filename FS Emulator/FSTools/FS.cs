using FS_Emulator.FSTools.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
	public partial class FS
	{
		public int FSNameSize = 30;
		public int MaxUsersCount = 64;
		public int MaxFilesCount;
		public Stream stream;
		public int CurrentUserId;
		public int UsersDirIndex = 2;
		//public int CurrentDirId = 2; // Да, в идеале каждого отдельного юзера посылать в его директорию... Но это уже пространство для улучшения. Потом.
		public const int RootUserId = 0;
		public const int MFTFileIndex = 1;
		public const int UsersFileIndex = 4;
		public const int offsetForServiceZone = 0;

		public FS()
		{

		}

		public FS(string path)
		{
			Open(path);
		}

		public void Open(string path)
		{
			stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
		}



		public void FormatOrCreate(string path, int capacityInMegabytes, int blockSizeInBytes)
		{

			// создать BlocksFreeOrBusy
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);
			int blocksCountForZoneOfFreeOrUsedBlocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);

			// создать Users
			var usersZoneBytes = GetNewUsersZoneBytes();
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, usersZoneBytes.Length);


			try
			{
				FormatFSFile(path, blockSizeInBytes, capacityInMegabytes);
			}
			catch (IOException)
			{
				System.Windows.Forms.MessageBox.Show("Не удается создать или записать файл.");
			}
		}

		public byte[] GetNewUsersZoneBytes()
		{
			var usersZoneBytes = new List<byte>();
			var firstUser = new UserRecord(0, "Root", "root", "root");
			var secondUser = new UserRecord(1, "Admin", "admin", "admin");

			var bytes = new List<byte>();
			bytes.AddRange(firstUser.ToBytes());
			bytes.AddRange(secondUser.ToBytes());

			return bytes.ToArray();
		}

		/// <summary>
		/// Создает структуру ФС в заданном файле. Пересоздает файл, если он есть.
		/// </summary>
		/// <param name="path">Путь к файлу</param>
		/// <param name="blockSizeInBytes">Размер кластера/блока на "диске" в байтах</param>
		/// <param name="capacityInMegabytes">Емкость "диска" в мегабайтах</param>
		public void FormatFSFile(string path, int blockSizeInBytes, int capacityInMegabytes)
		{
			/*Короч, я понял.
			 Вручную:
			 заполняю RootDir with MFT
			 MFT*/
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);
			var usersBytes = GetNewUsersZoneBytes();
			var numberOfBlocks = Bytes.FromMegabytes(capacityInMegabytes) / blockSizeInBytes;


			#region Заполнение данных rootDir
			var fileHeaders = new FileHeader[] {
				new FileHeader(1,0),
				new FileHeader(0,0), // В корневой директории; Удаленный файл (IndexInMFT==0)
				new FileHeader(3,0),
				new FileHeader(4,0)
			};

			var listHeadersBytes = new List<byte>();
			foreach (var header in fileHeaders)
			{
				listHeadersBytes.AddRange(header.ToBytes());
			}
			#endregion

			#region Создание записей MFT
			var blocksCountForServiceZone = 1; // никак больше не получится. Она там весит 75Б
			var blockServiceZoneFrom = 0;
			var blockServiceZoneTo = blockServiceZoneFrom + blocksCountForServiceZone;

			#region List_blocks
			var blocksCountForList_BlocksZone = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);
			var blockZoneOfList_BlocksFrom = blockServiceZoneTo;
			var blockZoneOfList_BlocksTo = blockZoneOfList_BlocksFrom + blocksCountForList_BlocksZone;

			var dataForList_BlocksRecordBytes = new List<byte>();
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blockZoneOfList_BlocksFrom));
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blockZoneOfList_BlocksTo));

			var List_BlocksRecord = new MFTRecord(3, "$List_Blocks", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, UserRights.OnlyOwnerRights), dataForList_BlocksRecordBytes.ToArray(), isNotInMFT: true);
			List_BlocksRecord.FileSize = list_BlocksBytes.Count();
			#endregion

			#region Users


			var blockUsersZoneFrom = blockZoneOfList_BlocksTo;
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, UserRecord.SizeInBytes * MaxUsersCount);
			var blockUsersZoneTo = blockUsersZoneFrom + blocksCountForUsersZone;

			var dataForUsersRecordBytes = new List<byte>();
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneFrom));
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneTo));

			var UsersRecord = new MFTRecord(4, "$Users", "$.", FileType.Bin, UserRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, UserRights.OnlyOwnerRights), dataForUsersRecordBytes.ToArray(), isNotInMFT: true);

			#endregion

			#region MFT


			var blockCountForFullMFTZone = (int)((double)list_BlocksBytes.Length / 100 * 12); //12% всех блоков - на MFT зону.
			var blockDataFrom = blockCountForFullMFTZone;
			var blocksForMFT = blockCountForFullMFTZone - (blocksCountForUsersZone + blocksCountForList_BlocksZone);
			var bytesForMFT = blocksForMFT * blockSizeInBytes;
			var maxFilesCount = bytesForMFT / MFTRecord.SizeInBytes;
			MaxFilesCount = maxFilesCount;

			var blockMFTRecordFrom = blockUsersZoneTo;
			var blockMFTRecordTo = blockDataFrom;

			var dataForMFTRecordBytes = new List<byte>();
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordFrom));
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordTo));

			var MftRecord = new MFTRecord(1, "$MFT", "$.", FileType.Bin, MFTRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, UserRights.OnlyOwnerRights), dataForMFTRecordBytes.ToArray(), isNotInMFT: true);
			#endregion
			var RootDirRecord = new MFTRecord(0, "$.", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new UserRights(0, UserRights.OnlyOwnerRights), listHeadersBytes.ToArray());
			#endregion

			var list_BlocksRecordsBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);

			var usersRecordsBytes = GetNewUsersZoneBytes();

			#region Заполнение size в записях MFT
			RootDirRecord.FileSize = listHeadersBytes.Count;
			List_BlocksRecord.FileSize = list_BlocksRecordsBytes.Length;
			UsersRecord.FileSize = usersRecordsBytes.Length;
			MftRecord.FileSize = MFTRecord.SizeInBytes * 5;
			#endregion

			#region Создание исходных данных для MFT
			var listMFTBytes = new List<byte>();
			listMFTBytes.AddRange(RootDirRecord.ToBytes());
			listMFTBytes.AddRange(MftRecord.ToBytes());

			var _2ndMFTRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new UserRights(0, UserRights.OnlyOwnerRights), listHeadersBytes.ToArray())
			{
				Index = 2,
				IsFileExists = false
			};

			listMFTBytes.AddRange(_2ndMFTRecord.ToBytes());
			listMFTBytes.AddRange(List_BlocksRecord.ToBytes());
			listMFTBytes.AddRange(UsersRecord.ToBytes());
			var MFTRecordsBytes = listMFTBytes.ToArray();
			#endregion



			#region Запись на "диск"
			var ServiceZoneOffsetInBytes = blockServiceZoneFrom * blockSizeInBytes;
			var List_BlocksOffsetInBytes = blockZoneOfList_BlocksFrom * blockSizeInBytes;
			var UsersOffsetInBytes = blockUsersZoneFrom * blockSizeInBytes;
			var MFTOffsetInBytes = blockMFTRecordFrom * blockSizeInBytes;

			var serviceRecord = new ServiceRecord(blockSizeInBytes, blockMFTRecordFrom, blockDataFrom)
			{
				Max_files_count = maxFilesCount,
				Max_files_count_InsideDir = MFTRecord.SpaceForData / FileHeader.SizeInBytes,
				Files_count = 4,
				Max_users_count = 64,
				Number_Of_Blocks = numberOfBlocks,
				Number_Of_Free_Blocks = numberOfBlocks,
			};
			serviceRecord.Users_count = 2; // root and admin

			var serviceBytes = serviceRecord.ToBytes();

			// stream?.Dispose();
			/*if (stream!=null)
			{
				using (stream) { }
			}*/

			using (stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				stream.Position = ServiceZoneOffsetInBytes;
				stream.Write(serviceBytes, 0, serviceBytes.Length);

				stream.Position = List_BlocksOffsetInBytes;
				stream.Write(list_BlocksRecordsBytes, 0, list_BlocksRecordsBytes.Length);

				stream.Position = UsersOffsetInBytes;
				stream.Write(usersRecordsBytes, 0, usersRecordsBytes.Length);

				stream.Position = MFTOffsetInBytes;
				stream.Write(MFTRecordsBytes, 0, MFTRecordsBytes.Length);


				//тест
				byte[] rootDirBytes = GetFileDataByMFTRecord(GetMFTRecordByIndex(0));

				// создаю файлы пользователей в корне. Пока что тут только Admin
				var result = CreateDir(0, "Users", 0); // от имени root.
				ChangeFileRights(2, UserRights.AllRights); // должно было создать с индексом 2

				int mftSize = GetMFTSize();
				int rootDirSize = GetFileSize(0);

				result = CreateDir(2, "Admin", 1); // от имени Admin. Права по умолчанию, OnlyMe.

				var mf = GetMaxFilesCount();

				CreateFile(2, "afile", FileType.Text, 1);

				rootDirSize = GetFileSize(0);
				var usersDirSize = GetFileSize(2);
				var strings = GetExistingFilesInDirInShortForm(2);


				// посткриптум
				var listBusyBlocks = new List<int>();
				for (var i = 0; i < GetBlockStartData(); i++)
				{
					listBusyBlocks.Add(i);
				}
				SetBlocksBusy(listBusyBlocks);
			}

			#endregion


			

		}

		internal bool UserCanReadFile(int fileIndex, int userId)
		{
			return UserCanReadFile(GetMFTRecordByIndex(fileIndex), userId);
		}

		internal string GetFilePath(string path)
		{
			if (path.Length <= 3)
			{
				return "";
			}

			int index;

			int i = path.Length - 2;
			while (path[i] != '/')
				i--;
			index = i;

			string dirName = new string(path.Take(index).ToArray());
			return dirName;

		}

		internal string GetFileName(string path)
		{
			if (path.Length <= 2)
			{
				return path;
			}

			int index;

			int i = path.Length - 2;
			while (path[i] != '/')
				i--;
			index = i;

			string fileName = new string(path.Skip(index + 1).ToArray());
			return fileName;

		}

		internal FileType GetFileTypeByRecord(byte[] record)
		{
			if (record.Length != MFTRecord.SizeInBytes)
				throw new ArgumentException("Не то кол-во байт", nameof(record));
			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForFileType;
				byte fileTypeByte = (byte)ms.ReadByte();
				FileType result = (FileType)fileTypeByte;
				return result;
			}
		}

		internal int GetMFTIndexByPathAndFileName(string path, string fileName)
		{
			var pathBytes = path.ToBytes();
			var fileNameBytes = fileName.ToBytes().TrimOrExpandTo(MFTRecord.LengthOfFileName);

			var record = FindMFTRecord(pathBytes, fileNameBytes);
			if (record is null)
				return -1;
			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForIndex;
				var indexBytes = new byte[4];
				ms.Read(indexBytes, 0, indexBytes.Length);

				int index = BitConverter.ToInt32(indexBytes, 0);
				return index;
			}
		}

		internal string GetFullFilePathByMFTIndex(int fileIndex)
		{
			byte[] record = GetMFTRecordByIndex(fileIndex);
			string path = GetFullFilePathByMFTRecord(record);

			return path;
		}

		internal void Close()
		{
			stream.Close();
		}

		public string[] GetAllExistingUsersStrings()
		{
			List<UserRecord> users = GetAllExistingUsers().ToList();
			var res = new List<string>();
			foreach (var user in users)
			{
				res.Add(user.ToString());
			}

			return res.ToArray();
		}

		public UserRecord GetUserByLogin(string login)
		{
			List<UserRecord> users = GetAllExistingUsers().ToList();
			foreach (var user in users)
			{
				if (user.Login.SequenceEqual(login.ToBytes().TrimOrExpandTo(UserRecord.LengthForLogin)))
				{
					return user;
				}
			}

			return default;
		}

		public UserRecord GetUserById(int userId)
		{
			List<UserRecord> users = GetAllExistingUsers().ToList();
			foreach (var user in users)
			{
				if (user.User_id == userId)
				{
					return user;
				}
			}

			return default;
		}

		private UserRecord[] GetAllExistingUsers()
		{
			byte[] usersData = GetFileDataByMFTIndex(UsersFileIndex);
			var list = new List<UserRecord>();
			using (var ms = new MemoryStream(usersData))
			{
				while (ms.Position < ms.Length)
				{
					var userRecBytes = new byte[UserRecord.SizeInBytes];
					ms.Read(userRecBytes, 0, userRecBytes.Length);
					var record = UserRecord.FromBytes(userRecBytes);
					if (record.IsUserExists)
					{
						list.Add(record);
					}
				}
			}

			return list.ToArray();
		}

		private UserRights GetFileOwnerRights(int mftFileIndex)
		{
			var offset = GetMFTRecordOffsetByIndex(mftFileIndex);
			offset += MFTRecord.OffsetForOwnerRights;

			stream.Position = offset;
			var bytes = new byte[UserRights.SizeInBytes];
			stream.Read(bytes, 0, bytes.Length);
			return UserRights.FromBytes(bytes);
		}

		internal bool UserCanWriteFile(int fileIndex, int userId)
		{
			return UserCanWriteFile(GetMFTRecordByIndex(fileIndex), userId);
		}

		/// <summary>
		/// Меняет владельца файла на нового. Подразумевается, что операция делается из-под Root.
		/// </summary>
		/// <param name="mftIndex">Индекс файла в MFT, владельца которого надо поменять</param>
		/// <param name="newUserId">UserId нового пользователя.</param>
		public void ChangeFileOwner(int mftIndex, short newUserId)
		{
			int recOffset = GetMFTRecordOffsetByIndex(mftIndex);

			stream.Position = recOffset;
			stream.Position += MFTRecord.OffsetForOwnerRights;
			stream.Position += UserRights.OffsetForUserId;

			// нужно 2 байта. Поэтому обязательно short.
			var bytes = BitConverter.GetBytes(newUserId);
			stream.Write(bytes, 0, bytes.Length);
		}

		

		/// <summary>
		/// Меняет права владельца файла. Без проверок.
		/// </summary>
		/// <param name="mftIndex">Индекс файла в MFT</param>
		/// <param name="newRights">Новые права.</param>
		public void ChangeFileRights(int mftIndex, short newRights)
		{
			int recOffset = GetMFTRecordOffsetByIndex(mftIndex);

			stream.Position = recOffset;
			stream.Position += MFTRecord.OffsetForOwnerRights;
			stream.Position += UserRights.OffsetForRights;

			// нужно 2 байта. Поэтому обязательно short.
			var bytes = BitConverter.GetBytes(newRights);
			stream.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Добавляет нового пользователя.
		/// </summary>
		/// <param name="name">Имя пользователя</param>
		/// <param name="login">Уникальный логин пользователя</param>
		/// <param name="password">Незашифрованный пароль пользователя</param>
		/// <returns>Результат создания пользователя</returns>
		public CreateUserResult AddUser(string name, string login, string password)
		{
			login = login.ToLower();
			// Если уже макс. кол-во юзеров, то return MaxUsersCountReached
			if (GetUsersCount() == GetMaxUsersCount())
				return CreateUserResult.MaxUsersCountReached;

			if (GetIsUserExistsByLogin(login))
			{
				return CreateUserResult.UserAlreadyExists;
			}


			int usersStart = GetByteStartUsers();
			int startPosition = usersStart;
			int endPosition = startPosition + GetFileSize(UsersFileIndex);
			stream.Position = startPosition;
			byte[] checkedRecord = new byte[UserRecord.SizeInBytes];

			bool checkedRecordExists;
			do
			{
				checkedRecordExists = true;

				stream.Read(checkedRecord, 0, checkedRecord.Length);

				if (!GetIsUserExists(checkedRecord))
				{
					checkedRecordExists = false;
				}
			} while (stream.Position < endPosition && checkedRecordExists);
			//нашел место. Теперь записать сюда данные. Индекс, бла-бла-бла. Индекс в любом случае можно взять из Position.

			if (!checkedRecordExists)
			{
				stream.Position -= UserRecord.SizeInBytes;
			}

			#region Создание записи
			short userId = (short)((stream.Position - startPosition) / UserRecord.SizeInBytes);
			byte[] newRecord = new UserRecord(userId, name, login, password).ToBytes();
			#endregion
			stream.Write(newRecord, 0, newRecord.Length);

			if (checkedRecordExists)
			{
				IncreaseUsersFileSize();
			}

			// И не забыть еще увеличить UsersCount в Service.
			IncreaseUsersCount();

			if (CreateDir(2, login, userId) != CreateFileResult.OK)
				return CreateUserResult.OKButCanNotCreateUserDir;

			return CreateUserResult.OK;
		}

		internal ServiceRecord GetServiceRecord()
		{
			stream.Position = 0;
			var buf = new byte[ServiceRecord.SizeInBytes];
			stream.Read(buf, 0, buf.Length);
			return ServiceRecord.FromBytes(buf);
		}

		private void IncreaseUsersFileSize()
		{
			var offset = GetMFTRecordOffsetByIndex(UsersFileIndex);
			offset += MFTRecord.OffsetForFileSize;

			var buf = new byte[4];
			stream.Position = offset;
			stream.Read(buf, 0, buf.Length);

			int oldFileSize = BitConverter.ToInt32(buf, 0);
			int newFileSize = oldFileSize + UserRecord.SizeInBytes;
			buf = BitConverter.GetBytes(newFileSize);

			stream.Position -= buf.Length;
			stream.Write(buf, 0, buf.Length);
		}

		private bool GetIsUserExistsByLogin(string login)
		{
			var loginBytes = login.ToBytes().TrimOrExpandTo(UserRecord.LengthForLogin);

			var users = GetAllExistingUsers();
			foreach (var user in users)
			{
				if (user.Login.SequenceEqual(loginBytes))
					return true;
			}

			return false;
		}

		private bool GetIsLoginCorrespondsUser(string login, byte[] checkedRecord)
		{
			var loginBytes = login.ToBytes().TrimOrExpandTo(UserRecord.LengthForLogin);
			using (var ms = new MemoryStream(checkedRecord))
			{
				ms.Position = UserRecord.OffsetForLogin;
				var readBytes = new byte[UserRecord.LengthForLogin];
				ms.Read(readBytes, 0, readBytes.Length);
				if (readBytes.SequenceEqual(loginBytes))
					return true;
				else
					return false;
			}
		}

		private bool GetIsUserExists(byte[] userRecord)
		{
			if (userRecord.Length < UserRecord.SizeInBytes)
				throw new ArgumentException("Кол-во байт не соответствует норме", nameof(userRecord));
			using (var ms = new MemoryStream(userRecord))
			{
				ms.Position = UserRecord.OffsetForIsUserExists;
				var buf = new byte[1];
				ms.Read(buf, 0, buf.Length);

				bool exists = BitConverter.ToBoolean(buf, 0);

				return exists;
			}
		}

		private int GetMFTRecordOffsetByIndex(int mftIndex)
		{
			int offset = GetByteStartMFT();
			offset += mftIndex * MFTRecord.SizeInBytes;

			return offset;
		}

		public int GetBlockCountForBytes(int blockSizeInBytes, int bytesCount)
		{
			var res = bytesCount / blockSizeInBytes;
			var temp = bytesCount % blockSizeInBytes;

			if (temp == 0)
			{
				return res;
			}
			else
			{
				return res + 1;
			}
		}

		public byte[] GetNewBlocksFreeOrUsedZoneBytes(int capacityInMegabytes, int clusterSizeInBytes)
		{
			var numberOfBlocks = Bytes.FromMegabytes(capacityInMegabytes) / clusterSizeInBytes;

			var bits = new BitArray(numberOfBlocks);
			byte[] bytes;

			if (numberOfBlocks % 8 == 0)
			{
				bytes = new byte[bits.Length / 8];
			}
			else
			{
				bytes = new byte[bits.Length / 8 + 1];
			}

			bits.CopyTo(bytes, 0);
			return bytes;
		}


		/// <summary>
		/// Находит MFTRecord с заданным path и fileName.
		/// </summary>
		/// <param name="path">путь к файлу</param>
		/// <param name="fileName">имя файла</param>
		/// <returns>MFTRecord. null, если не найдено.</returns>
		public byte[] FindMFTRecord(byte[] path, byte[] fileName)
		{
			path = path.ToNormalizedPath().TrimOrExpandTo(206);


			fileName = fileName.TrimOrExpandTo(50);


			var byteStartMFT = GetByteStartMFT();
			int MFTSize = GetMFTSize();

			{
				stream.Position = 0;



				stream.Position = byteStartMFT;

				var recBytes = new byte[MFTRecord.SizeInBytes];
				var fname = new byte[50];
				var fpath = new byte[206];
				var isExistsBuffer = new byte[1];
				do
				{
					stream.Read(recBytes, 0, recBytes.Length);

					using (var ms = new MemoryStream(recBytes))
					{
						ms.Position = MFTRecord.OffsetForIsFileExists;
						ms.Read(isExistsBuffer, 0, isExistsBuffer.Length);
						bool FileExists = BitConverter.ToBoolean(isExistsBuffer, 0);
						if (!FileExists)
							continue;

						ms.Position = MFTRecord.OffsetForFileName;
						ms.Read(fname, 0, fname.Length);
						ms.Position = MFTRecord.OffsetForPath;
						ms.Read(fpath, 0, fname.Length);
					}

					if (fname.SequenceEqual(fileName))
						if (fpath.SequenceEqual(path))
							return recBytes; // нашли файл
				} while (stream.Position - byteStartMFT < MFTSize);
			}

			// Баг: не находит $Users. Хм, два раза находит List_Blocks???
			return null; // такой не найден
		}

		public byte[] FindMFTRecord(int mftIndex)
		{
			var maxFilesCount = GetMaxFilesCount();
			if (mftIndex >= maxFilesCount)
				return null;

			var mftStart = GetByteStartMFT();
			// позиция = байт, где начало нужной записи
			stream.Position = mftStart + mftIndex * MFTRecord.SizeInBytes;
			var recBuf = new byte[MFTRecord.SizeInBytes];
			stream.Read(recBuf, 0, recBuf.Length);

			var exists = recBuf[MFTRecord.OffsetForIsFileExists];
			if (exists == 0)
				return null;

			return recBuf;
		}


		public int GetBlockSizeInBytes()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlockSizeInBytes;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var blockSizeInBytes = BitConverter.ToInt32(buf, 0);
			return blockSizeInBytes;
		}

		/// <summary>
		/// Возвращает блок, где начинается MFT
		/// </summary>
		/// <returns></returns>
		public int GetBlockStartMFT()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlock_start_MFT;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var mftLocationBlock = BitConverter.ToInt32(buf, 0);
			return mftLocationBlock;
		}

		public int GetByteStartMFT()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlock_start_MFT;

			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);

			var mftLocationBlock = BitConverter.ToInt32(buf, 0);
			var blockSizeInBytes = GetBlockSizeInBytes();
			var byteStartMFT = mftLocationBlock * blockSizeInBytes;

			return byteStartMFT;
		}

		

		/// <summary>
		/// Возвращает блок, где начинается Data
		/// </summary>
		/// <returns></returns>
		public int GetBlockStartData()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlock_start_Data;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var blockStartData = BitConverter.ToInt32(buf, 0);
			return blockStartData;
		}

		public int GetByteStartData()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlock_start_Data;

			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);

			var mftLocationBlock = BitConverter.ToInt32(buf, 0);
			var blockSizeInBytes = GetBlockSizeInBytes();
			var byteStartData = mftLocationBlock * blockSizeInBytes;

			return byteStartData;
		}

		public int GetMaxFilesCount()
		{
			stream.Position = 0 + ServiceRecord.OffsetForMax_files_count;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var maxFilesCount = BitConverter.ToInt32(buf, 0);
			return maxFilesCount;
		}

		public int GetMaxUsersCount()
		{
			stream.Position = 0 + ServiceRecord.OffsetForMax_users_count;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var maxUsersCount = BitConverter.ToInt32(buf, 0);
			return maxUsersCount;
		}

		public int GetFilesCount()
		{
			stream.Position = 0 + ServiceRecord.OffsetForFiles_count;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var filesCount = BitConverter.ToInt32(buf, 0);
			return filesCount;
		}

		public void SetFilesCount(int newFilesCount)
		{
			stream.Position = 0 + ServiceRecord.OffsetForFiles_count;
			var buf = BitConverter.GetBytes(newFilesCount);
			stream.Write(buf, 0, buf.Length);
		}

		public int GetUsersCount()
		{
			stream.Position = 0 + ServiceRecord.OffsetForUsers_count;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var usersCount = BitConverter.ToInt32(buf, 0);
			return usersCount;
		}

		public void SetUsersCount(int newUsersCount)
		{
			stream.Position = 0 + ServiceRecord.OffsetForUsers_count;
			var buf = BitConverter.GetBytes(newUsersCount);
			stream.Write(buf, 0, buf.Length);
		}

		public long GetNumber_Of_Blocks()
		{
			stream.Position = 0 + ServiceRecord.OffsetForNumber_Of_Blocks;
			var buf = new byte[8];
			stream.Read(buf, 0, buf.Length);
			var number_Of_Blocks = BitConverter.ToInt64(buf, 0);
			return number_Of_Blocks;
		}

		public long GetNumber_Of_Free_Blocks()
		{
			stream.Position = 0 + ServiceRecord.OffsetForNumber_Of_Free_Blocks;
			var buf = new byte[8];
			stream.Read(buf, 0, buf.Length);
			var number_Of_Free_Blocks = BitConverter.ToInt64(buf, 0);
			return number_Of_Free_Blocks;
		}

		public void SetNumber_Of_Free_Blocks(long newNumber_Of_Free_Blocks)
		{
			stream.Position = 0 + ServiceRecord.OffsetForNumber_Of_Free_Blocks;
			var buf = BitConverter.GetBytes(newNumber_Of_Free_Blocks);
			stream.Write(buf, 0, buf.Length);
		}

		public byte[] GetFS_Version()
		{
			stream.Position = 0 + ServiceRecord.OffsetForFS_Version;
			var buf = new byte[30];
			stream.Read(buf, 0, buf.Length);
			var fS_Version = buf;
			return fS_Version;
		}

		public byte GetVolume()
		{
			stream.Position = 0 + ServiceRecord.OffsetForVolume;
			var buf = new byte[1];
			stream.Read(buf, 0, buf.Length);
			var volume = buf[0];
			return volume;
		}


		/// <summary>
		/// Возвращает размер MFT в байтах
		/// </summary>
		/// <returns>размер MFT в байтах</returns>
		public int GetMFTSize()
		{
			var blockSizeInBytes = GetBlockSizeInBytes();
			var blockStartMFT = GetBlockStartMFT();
			// перехожу к записи MFT

			stream.Position = blockStartMFT * blockSizeInBytes + MFTRecord.SizeInBytes;
			stream.Position += MFTRecord.OffsetForFileSize;

			var buffer = new byte[4];
			stream.Read(buffer, 0, buffer.Length);
			var MFTSize = BitConverter.ToInt32(buffer, 0);
			return MFTSize;
		}

		public byte[] GetMFTRecordByIndex(int index)
		{
			var offset = GetByteStartMFT() + index * MFTRecord.SizeInBytes;
			stream.Position = offset;
			var recBytes = new byte[MFTRecord.SizeInBytes];
			stream.Read(recBytes, 0, recBytes.Length);

			return recBytes;
		}

		public bool CheckPathExists(string path)
		{
			// var pathBytes = path.ToNormalizedPath().ToBytes().TrimOrExpandTo(206);
			// path - это pathToDir + fileName
			// так что я могу сделать FindFile(path, name)
			var pathForDir = Path.GetDirectoryName(path).ToNormalizedPath().ToBytes().TrimOrExpandTo(206);
			var fnameDir = Path.GetFileName(path).ToBytes().TrimOrExpandTo(50);


			return false;
		}


		public CreateFileResult CreateFile(int parentDirIndex, string fileName, FileType fileType, int userId, int size_Unit = 1, byte[] data = null)
		{
			if (!ValidateFileName(fileName))
			{
				return CreateFileResult.InvalidFileName;
			}

			if (data == null)
				data = new byte[0];
			/*Создать заголовок, вставить его в директорию.
			 Создать MFTRecord, вставить ее в MFT
			 Заголовок нужен только для отображения в директории!*/

			// Какие условия, чтобы не удалось создать?
			/*
			 path not exists
			 file already exists
			 not enough rights
			 not enough space // Может быть только если директории придется расширяться за пределы блока.
			 max files number reached
			 */
			var parentDirRecord = GetMFTRecordByIndex(parentDirIndex);
			if (parentDirRecord == null)
			{
				return CreateFileResult.DirNotExists;
			}

			if (GetMFTIndexOfExistingFileByParentDirAndFileName(parentDirIndex, fileName) >= 0)
			{
				return CreateFileResult.FileAlreadyExists;
			}

			if (!UserCanWriteFile(parentDirRecord, userId))
			{
				return CreateFileResult.NotEnoughRights;
			}

			if (!(GetFilesCount() < GetMaxFilesCount())) // уже создано файлов >= макс. кол-ва
			{
				return CreateFileResult.MaxFilesNumberReached;
			}

			// индекс == 5, все верно
			int indexOfNewFile = AddMFTRecord(fileName, GetFullFilePathByMFTRecord(parentDirRecord), fileType, size_Unit, DateTime.Now, DateTime.Now, FileFlags.None, new UserRights((short)userId, UserRights.OnlyOwnerRights));
			//		private int AddNewMFTRecord(string fileName, string path, FileType fileType, int unitSize, DateTime timeCreation, DateTime timeModification, byte flags, UserRights userRights)
			//

			// индекс в MFT получили, теперь добавить его в директорию
			var addToDirResult = AddFileHeaderToDir(parentDirIndex, indexOfNewFile);
			if (addToDirResult == ModifyFileResult.NotEnoughSpace)
			{
				return CreateFileResult.NotEnoughSpace;
			}

			return CreateFileResult.OK;
		}

		public bool ValidateFileName(string fileName)
		{
			if (fileName.Contains('/'))
				return false;
			if (fileName.Length == 0 || fileName.Length > MFTRecord.LengthOfFileName)
				return false;

			return true;
		}

		/// <summary>
		/// Создает запись MFT и возвращает ее индекс. Нет проверок на "можно ли добавить запись"
		/// </summary>
		/// <param name="fileName"></param>
		private int AddMFTRecord(string fileName, string path, FileType fileType, int unitSize, DateTime timeCreation, DateTime timeModification, byte flags, UserRights userRights)
		{
			path = path.ToNormalizedPath();

			int mftStart = GetByteStartMFT();
			int startPosition = mftStart;
			int endPosition = startPosition + GetFileSize(MFTFileIndex);//Надо в MFT менять FileSize. И использовать его здесь. А то пустое место почему-то не читает.
			stream.Position = startPosition;
			byte[] checkedRecord = new byte[MFTRecord.SizeInBytes];

			bool checkedRecordExists;
			do
			{
				checkedRecordExists = true;

				stream.Read(checkedRecord, 0, checkedRecord.Length);

				if (!GetIsFileExists(checkedRecord))
				{
					checkedRecordExists = false;
				}
			} while (stream.Position < endPosition && checkedRecordExists);
			//нашел место. Теперь записать сюда данные. Индекс, бла-бла-бла. Индекс в любом случае можно взять из Position.

			if (!checkedRecordExists)
			{
				stream.Position -= MFTRecord.SizeInBytes;
			}


			#region Создание записи
			int recIndex = ((int)stream.Position - startPosition) / MFTRecord.SizeInBytes;
			byte[] newRecord = new MFTRecord(recIndex, fileName, path, fileType, unitSize, timeCreation, timeModification, flags, userRights).ToBytes();
			#endregion
			stream.Write(newRecord, 0, newRecord.Length);

			if (checkedRecordExists)
			{
				IncreaseMFTFileSize();
			}
			// И не забыть еще увеличить FilesCount в Service.
			IncreaseFilesCount();
			return recIndex;
		}

		private void IncreaseMFTFileSize()
		{
			var offset = GetMFTRecordOffsetByIndex(MFTFileIndex);
			offset += MFTRecord.OffsetForFileSize;

			var buf = new byte[4];
			stream.Position = offset;
			stream.Read(buf, 0, buf.Length);

			int oldFileSize = BitConverter.ToInt32(buf, 0);
			int newFileSize = oldFileSize + MFTRecord.SizeInBytes;
			buf = BitConverter.GetBytes(newFileSize);

			stream.Position -= buf.Length;
			stream.Write(buf, 0, buf.Length);
		}

		/// <summary>
		/// Увеличивает значение кол-ва файлов в системе на указанное кол-во.
		/// </summary>
		/// <param name="count">Число, на которое нужно увеличить</param>
		/// <returns>Новое кол-во файлов</returns>
		private int IncreaseFilesCount(int count = 1)
		{
			int startPosition = offsetForServiceZone + ServiceRecord.OffsetForFiles_count;

			// считывание FilesCount
			stream.Position = startPosition;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			int oldfCount = BitConverter.ToInt32(buf, 0);

			var newfCount = oldfCount + count;
			byte[] newfCountBytes = BitConverter.GetBytes(newfCount);

			// Запись нового FilesCount
			stream.Position = startPosition;
			stream.Write(newfCountBytes, 0, newfCountBytes.Length);

			return newfCount;
		}

		/// <summary>
		/// Уменьшает значение кол-ва файлов в системе на указанное кол-во.
		/// </summary>
		/// <param name="count">Число, на которое нужно уменьшить</param>
		/// <returns>Новое кол-во файлов</returns>
		private int DecreaseFilesCount(int count = 1)
		{
			int startPosition = offsetForServiceZone + ServiceRecord.OffsetForFiles_count;

			// считывание FilesCount
			stream.Position = startPosition;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			int oldfCount = BitConverter.ToInt32(buf, 0);

			var newfCount = oldfCount - count;
			byte[] newfCountBytes = BitConverter.GetBytes(newfCount);

			// Запись нового FilesCount
			stream.Position = startPosition;
			stream.Write(newfCountBytes, 0, newfCountBytes.Length);

			return newfCount;
		}

		private void IncreaseUsersCount()
		{
			int startPosition = offsetForServiceZone + ServiceRecord.OffsetForUsers_count;

			// считывание FilesCount
			stream.Position = startPosition;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			int olduCount = BitConverter.ToInt32(buf, 0);

			var newUsersCount = olduCount + 1;
			byte[] newUsersCountBytes = BitConverter.GetBytes(newUsersCount);

			// Запись нового UsersCount
			stream.Position = startPosition;
			stream.Write(newUsersCountBytes, 0, newUsersCountBytes.Length);
		}

		public string GetFullFilePathByMFTRecord(byte[] record)
		{
			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForPath;
				var bufPath = new byte[MFTRecord.LengthOfPath];
				ms.Read(bufPath, 0, bufPath.Length);
				var shortPath = bufPath.ToASCIIString();

				ms.Position = MFTRecord.OffsetForFileName;
				var bufFName = new byte[MFTRecord.LengthOfFileName];
				ms.Read(bufFName, 0, bufFName.Length);
				var shortFName = bufFName.ToASCIIString();

				return shortPath.ToNormalizedPath() + shortFName;
			}
		}

		/*/// <summary>
		/// Возвращает номер байта, с которого начинается файл Users
		/// </summary>
		private int GetByteStartUsers()
		{
			byte[] data = GetFileDataByMFTIndex(UsersFileIndex);
			using (var ms = new MemoryStream(data))
			{
				var buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				int blockNumber = BitConverter.ToInt32(buf, 0); // первый блок, начало
				int byteNumber = blockNumber * GetBlockSizeInBytes();
				return byteNumber;
			}
		}*/

		/// <summary>
		/// Добавляет в директорию запись о файле
		/// </summary>
		/// <param name="mftParentDirIndex">Индекс директории в MFT</param>
		/// <param name="mftFileIndex">Индекс добавляемого файла в MFT</param>
		/// <returns>Результат добавления записи в директорию</returns>
		public ModifyFileResult AddFileHeaderToDir(int mftParentDirIndex, int mftFileIndex)
		{
			// Идем по Data. Если находим запись с Index==0, то заменяем ее, возвращаем OK.
			// Что ж, мы дошли до конца данных (fileSize). Если (MFTRecord.SpaceForData - fileSize) < FileHeader.SizeInBytes, то return NotEnoughSpace. Иначе без проблем записываем в конец.
			int dirSize = GetFileSize(mftParentDirIndex);

			if (MFTRecord.SpaceForData - dirSize < FileHeader.SizeInBytes)
				return ModifyFileResult.NotEnoughSpace;

			int dirMFTOffset = GetByteStartMFT() + mftParentDirIndex * MFTRecord.SizeInBytes;

			stream.Position = dirMFTOffset;

			var startPosition = stream.Position + MFTRecord.OffsetForData;
			var endPosition = startPosition + dirSize;

			FileHeader newHeader;
			byte[] bytes;

			if (startPosition == endPosition)
			{ // записываем, увеличиваем размер и валим
				stream.Position = startPosition;
				newHeader = new FileHeader(mftFileIndex, mftParentDirIndex);
				bytes = newHeader.ToBytes();
				stream.Write(bytes, 0, bytes.Length);

				IncreaseDirSize(mftParentDirIndex);
				return ModifyFileResult.OK;
			}


			#region Собственно процесс поиска и добавления
			stream.Position = startPosition;

			FileHeader currHeader = default;

			bool isHeaderFileExists; // if true, надо вернуться чтобы записать на его место.
			do
			{
				isHeaderFileExists = true;

				var buf = new byte[FileHeader.SizeInBytes];
				stream.Read(buf, 0, buf.Length);
				currHeader = FileHeader.FromBytes(buf);

				if (currHeader.IndexInMFT == 0)
					isHeaderFileExists = false;
			} while (stream.Position < endPosition && isHeaderFileExists);


			if (!isHeaderFileExists)
			{
				stream.Position -= FileHeader.SizeInBytes;
			}

			newHeader = new FileHeader(mftFileIndex, mftParentDirIndex);
			bytes = newHeader.ToBytes();
			stream.Write(bytes, 0, bytes.Length);


			if (isHeaderFileExists) // значит, мы приписываем в конец
			{
				IncreaseDirSize(mftParentDirIndex);
			}


			#endregion

			return ModifyFileResult.OK;
		}

		private void IncreaseDirSize(int mftDirIndex)
		{
			int offset = GetMFTRecordOffsetByIndex(mftDirIndex);
			offset += MFTRecord.OffsetForFileSize;

			var buf = new byte[4];
			stream.Position = offset;
			stream.Read(buf, 0, buf.Length);

			int oldFileSize = BitConverter.ToInt32(buf, 0);
			int newFileSize = oldFileSize + FileHeader.SizeInBytes;
			buf = BitConverter.GetBytes(newFileSize);

			stream.Position -= buf.Length;
			stream.Write(buf, 0, buf.Length);

		}

		public int GetFileSize(int mftIndex)
		{
			var record = GetMFTRecordByIndex(mftIndex);
			if (!GetIsFileExists(record))
				throw new Exception("Файл удален");

			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForFileSize;
				var buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				return BitConverter.ToInt32(buf, 0);
			}
		}

		public bool GetIsFileExists(byte[] mftRecord)
		{
			if (mftRecord is null)
				return false;
			if (mftRecord.Length != MFTRecord.SizeInBytes)
				throw new ArgumentException("Кол-во байт не соответствует норме", nameof(mftRecord));
			using (var ms = new MemoryStream(mftRecord))
			{
				ms.Position = MFTRecord.OffsetForIsFileExists;
				var buf = new byte[1];
				ms.Read(buf, 0, buf.Length);
				return BitConverter.ToBoolean(buf, 0);
			}
		}

		public bool UserCanWriteFile(byte[] mftRecord, int userId)
		{
			var ownerRights = GetOwnerRightsFromRecord(mftRecord);
			var ownerId = ownerRights.UserId;

			if (userId != RootUserId) // Hi, I'm a regular user
			{
				if (ownerId == userId) // Hi, and I'm owner
				{
					if (!OwnerCanWrite(ownerRights.Rights))
						return false;
					else return true;
				}
				// другой юзер
				// Отлично, надо теперь проверить x-CanWrite.
				if (!OthersCanWrite(ownerRights.Rights)) // Hi, I'm a stranger, can I write in there?
					return false;
				else return true;
			}
			// I AM ROOOT!!!
			return true;
		}

		public bool UserCanReadFile(byte[] mftRecord, int userId)
		{
			// варианты: 1) это владелец (с установленными правами)
			//			 2) это другой пользователь, но владелец установил право записи для остальных.
			//			 0) это root, которому можно все.
			var ownerRights = GetOwnerRightsFromRecord(mftRecord);
			var ownerId = ownerRights.UserId;

			if (userId != RootUserId) // Hi, I'm a regular user
			{
				if (ownerId == userId) // Hi, I'm owner
				{
					if (!OwnerCanRead(ownerRights.Rights))
						return false;
					else
						return true;
				}
				// другой юзер
				else // // Hi, I'm a stranger, can I read in there?
				{
					if (!OthersCanRead(ownerRights.Rights))
						return false;
					else return true;
				}
			}
			// I AM ROOOT!!!
			return true;
		}

		public static bool OwnerCanWrite(short rights)
		{
			bool can = ((rights & UserRights.OwnerCanWriteRights) != 0);
			return can;
		}
		public static bool OthersCanWrite(short rights)
		{
			bool can = ((rights & UserRights.OthersCanWriteRights) != 0);
			return can;
		}


		public static bool OwnerCanRead(short rights)
		{
			bool can = ((rights & UserRights.OwnerCanReadRights) != 0);
			return can;
		}

		public static bool OthersCanRead(short rights)
		{
			bool can = ((rights & UserRights.OthersCanReadRights) != 0);
			return can;
		}

		public bool OwnerCanExecute(short rights)
		{
			var bools = new BitArray(rights);
			return bools[2] == true;
		}

		public bool OthersCanExecute(short rights)
		{
			var bools = new BitArray(rights);
			return bools[5] == true;
		}



		public string GetFileNameByIndex(int mftIndex)
		{
			var record = GetMFTRecordByIndex(mftIndex);
			if (record == null)
				throw new Exception("Все плохо");

			using (var ms = new MemoryStream(record))
			{
				byte[] fnameBytes = new byte[MFTRecord.LengthOfFileName];
				ms.Position = MFTRecord.OffsetForFileName;
				ms.Read(fnameBytes, 0, fnameBytes.Length);

				return fnameBytes.ToASCIIString().Replace("\0", "");
			}
		}

		/// <summary>
		/// Возвращает индекс файла в MFT по его имени, производя поиск в директории.
		/// </summary>
		/// <param name="parentDirIndex">номер в MFT, под которым зарегистрирована директория</param>
		/// <param name="fileName">имя файла, который ищем</param>
		/// <returns>номер файла в MFT или -1, если не найден</returns>
		public int GetMFTIndexOfExistingFileByParentDirAndFileName(int parentDirIndex, string fileName)
		{
			var fileNameBytes = fileName.ToBytes().TrimOrExpandTo(MFTRecord.LengthOfFileName);

			var mftDirRec = GetMFTRecordByIndex(parentDirIndex);
			if (mftDirRec == null)
				return -1;

			FileHeader[] existFilesHeaders = GetFileHeadersOfExistingFilesByDirIndex(parentDirIndex);
			foreach (var header in existFilesHeaders)
			{
				if (fileName == GetFileNameByIndex(header.IndexInMFT))
					return header.IndexInMFT;
			}

			// если так и не нашли
			return -1;
		}

		public byte[] GetFileDataByMFTIndex(int index)
		{
			return GetFileDataByMFTRecord(GetMFTRecordByIndex(index));
		}

		public byte[] GetFileDataByMFTRecord(byte[] record)
		{
			if (record.Length != MFTRecord.SizeInBytes)
			{
				throw new ArgumentException("Кол-во байтов в массиве не соответствует размеру MFTRecord", nameof(record));
			}

			var blocks = new List<int>();
			int fileSize;

			// If fully in MFT, here is return
			using (var recStream = new MemoryStream(record))
			{
				recStream.Position = MFTRecord.OffsetForIsNotInMFT;
				var isNotInMFT = recStream.ReadByte();

				recStream.Position = MFTRecord.OffsetForFileSize;
				var fsizeBytes = new byte[4];
				recStream.Read(fsizeBytes, 0, fsizeBytes.Length);
				fileSize = BitConverter.ToInt32(fsizeBytes, 0);
				if (fileSize == 0)
					return new byte[0];

				if (isNotInMFT == 0) // fully in MFT
				{
					#region Reading data and then return it
					recStream.Position = MFTRecord.OffsetForData;
					var data = new byte[fileSize];
					recStream.Read(data, 0, data.Length);
					return data;
					#endregion
				}


				#region Reading data as blocks numbers and filling the list of them
				// блок данных, который был считан на предыдущем шаге.
				byte[] alreadyReadBytes = new byte[4];
				var bytes = new byte[4];
				int block = 0;

				recStream.Position = MFTRecord.OffsetForData;
				do
				{
					recStream.Read(bytes, 0, bytes.Length);

					if (bytes.SequenceEqual(alreadyReadBytes) || bytes.SequenceEqual(new byte[4]))
					{
						break;
						/* Мы сюда попадаем только когда пытаемся считать пустое место 
						(не получается, в bytes остается то же, что и было)*/
					}
					bytes.CopyTo(alreadyReadBytes, 0);

					block = BitConverter.ToInt32(bytes, 0);
					if (block != 0)
					{
						blocks.Add(block);
					}

				} while (block != 0);
				#endregion
			}


			// Ok, we have blocks, now we need to read data from these blocks.
			#region Reading data from blocks and concatting it
			var blockSize = GetBlockSizeInBytes();
			var fileData = new List<byte>(fileSize);

			foreach (var blockNum in blocks)
			{
				stream.Position = blockNum * blockSize; // offset
				int neededBytesCount = fileSize - fileData.Count;

				byte[] bytesToAdd;
				if (neededBytesCount <= blockSize) // last block, may not be full
					bytesToAdd = new byte[fileSize - fileData.Count];
				else
					bytesToAdd = new byte[blockSize];

				stream.Read(bytesToAdd, 0, bytesToAdd.Length);
				fileData.AddRange(bytesToAdd);
			}

			#endregion
			return fileData.ToArray();
		}

		public UserRights GetOwnerRightsFromRecord(byte[] record)
		{
			if (record.Length != MFTRecord.SizeInBytes)
			{
				throw new ArgumentException("Кол-во байтов в массиве не соответствует размеру MFTRecord", nameof(record));
			}
			using (var recStream = new MemoryStream(record))
			{
				recStream.Position = MFTRecord.OffsetForOwnerRights;
				var buf = new byte[UserRights.SizeInBytes];
				recStream.Read(buf, 0, buf.Length);
				return UserRights.FromBytes(buf);
			}
		}

		public CreateFileResult CreateDir(int parentDirIndex, string dirName, int userId)
		{
			if ((GetFullFilePathByMFTIndex(parentDirIndex) + dirName).Length > MFTRecord.LengthOfPath)
				return CreateFileResult.PathTooLong;
			return CreateFile(parentDirIndex, dirName, FileType.Dir, userId, FileHeader.SizeInBytes);
		}

		public string[] GetExistingFilesInDirInShortForm(int dirIndex)
		{
			var res = new List<string>();

			var headers = GetFileHeadersOfExistingFilesByDirIndex(dirIndex);
			if (headers.Length == 0)
			{
				return new[] { "Тут пусто" };
			}

			foreach (var header in headers)
			{
				res.Add(GetShortFileInfo(header.IndexInMFT));
			}

			return res.ToArray();
		}

		public string GetShortFileInfo(int mftIndex)
		{
			// fileName, FileType, Time_Modification, FileSize, Owner, OwnerRights

			string fileName = default;
			FileType fileType = default;
			DateTime time_Modification = default;
			int fileSize = default;
			string ownerLogin = default;
			string ownerRights = default;

			var record = GetMFTRecordByIndex(mftIndex);
			if (record is null)
				throw new Exception("Все плохо");

			#region Считывание данных из записи
			byte[] buf;
			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForFileName;
				buf = new byte[MFTRecord.LengthOfFileName];
				ms.Read(buf, 0, buf.Length);
				fileName = buf.ToASCIIString().Replace("\0", "");

				ms.Position = MFTRecord.OffsetForFileType;
				buf = new byte[1];
				ms.Read(buf, 0, buf.Length);
				fileType = (FileType)buf[0];

				ms.Position = MFTRecord.OffsetForTime_Modification;
				buf = new byte[8]; // long
				ms.Read(buf, 0, buf.Length);
				time_Modification = BitConverter.ToInt64(buf, 0).ToDateTime();

				ms.Position = MFTRecord.OffsetForFileSize;
				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				fileSize = BitConverter.ToInt32(buf, 0);

				// ownerLogin. Сначала читаю индекс, потом по индексу получаю логин.
				ms.Position = MFTRecord.OffsetForOwnerRights + UserRights.OffsetForUserId;
				buf = new byte[2];
				ms.Read(buf, 0, buf.Length);
				var userId = BitConverter.ToInt16(buf, 0);
				ownerLogin = GetUserLoginById(userId);

				ms.Position = MFTRecord.OffsetForOwnerRights + UserRights.OffsetForRights;
				buf = new byte[2];
				ms.Read(buf, 0, buf.Length);
				var rights = BitConverter.ToInt16(buf, 0);
				ownerRights = rights.ToRightsString();
			}
			#endregion


			var shortFileInfo = new ShortFileInfo()
			{
				FileName = fileName,
				FileType = fileType,
				Time_Modification = time_Modification,
				FileSize = fileSize,
				OwnerLogin = ownerLogin,
				OwnerRights = ownerRights
			};

			return shortFileInfo.ToString();
		}

		private string GetUserLoginById(short userId)
		{
			byte[] userBytes = GetUserById(userId);
			if (userBytes is null)
				throw new Exception("Юзер с таким Id не найден. Проблемы.");

			using (var ms = new MemoryStream(userBytes))
			{
				var loginBytes = new byte[UserRecord.LengthForLogin];
				ms.Position = UserRecord.OffsetForLogin;
				ms.Read(loginBytes, 0, loginBytes.Length);

				string login = loginBytes.ToASCIIString().Replace("\0", "");
				//throw new NotImplementedException();
				return login;
			}
		}

		/// <summary>
		/// Возвращает СУЩЕСТВУЮЩЕГО пользователя по его Id. Если такого пользователя нет, возвращает null.
		/// </summary>
		private byte[] GetUserById(short userId)
		{
			int usersFileSize = GetUsersFileSize();
			int startFilePosition = GetByteStartUsers();
			int endFilePosition = startFilePosition + usersFileSize;

			int userRecordOffsetFromStartUsersFile = userId * UserRecord.SizeInBytes;

			if (usersFileSize <= userRecordOffsetFromStartUsersFile)
			{
				return null;
			}

			// теперь запись точно в файле.
			int offset = startFilePosition + userRecordOffsetFromStartUsersFile;

			stream.Position = offset;
			byte[] recBytes = new byte[UserRecord.SizeInBytes];
			stream.Read(recBytes, 0, recBytes.Length);

			// User может быть удален, и тогда его как бы нет
			if (!GetIsUserExists(recBytes))
				return null;

			return recBytes;
		}

		private int GetByteStartUsers()
		{
			// Файл 100% не в MFT, потому что я это задал при форматировании.
			byte[] recBytes = GetMFTRecordByIndex(UsersFileIndex);
			using (var ms = new MemoryStream(recBytes))
			{
				ms.Position = MFTRecord.OffsetForData;
				var buf = new byte[4];
				ms.Read(buf, 0, buf.Length); // номер первого блока файла

				int blockStart = BitConverter.ToInt32(buf, 0);
				int byteStart = blockStart * GetBlockSizeInBytes();

				return byteStart;
			}

		}

		private int GetUsersFileSize()
		{
			return GetFileSize(UsersFileIndex);
		}

		public FileHeader[] GetFileHeadersOfExistingFilesByDirIndex(int dirIndex)
		{
			var res = new List<FileHeader>();

			byte[] headersBytes = GetFileDataByMFTIndex(dirIndex);
			if (headersBytes.Length == 0)
			{
				return new FileHeader[0];
			}


			using (var ms = new MemoryStream(headersBytes))
			{
				while (ms.Position != ms.Length)
				{
					byte[] buf = new byte[FileHeader.SizeInBytes];
					ms.Read(buf, 0, FileHeader.SizeInBytes);

					var header = FileHeader.FromBytes(buf);

					if (header.IndexInMFT == 0) // если файл удален
						continue;

					res.Add(header);
				}
			}

			return res.ToArray();
		}

		/// <summary>
		/// Пытается переименовать файл в данной директории и возвращает результат.
		/// </summary>
		/// <param name="parentDirIndex">MFT индекс директории, в которой находится файл</param>
		/// <param name="oldName">имя файла, который надо переименовать</param>
		/// <param name="newName">новое имя файла</param>
		/// <param name="userId">UserId пользователя, от имени которого происходит переименование</param>
		/// <returns></returns>
		public CreateFileResult RenameFileInDir(int parentDirIndex, string oldName, string newName, int userId)
		{
			if (!ValidateFileName(oldName) || !ValidateFileName(newName))
			{
				return CreateFileResult.InvalidFileName;
			}

			var fileIndex = GetMFTIndexOfExistingFileByParentDirAndFileName(parentDirIndex, oldName);
			if (fileIndex < 0)
				return CreateFileResult.DirNotExists;

			var headers = GetFileHeadersOfExistingFilesByDirIndex(parentDirIndex);
			int targetFileIndex = -1;
			foreach (var header in headers)
			{
				var foundName = GetFileNameByIndex(header.IndexInMFT);
				if (foundName == newName)
					return CreateFileResult.FileAlreadyExists;

				if (foundName == oldName)
					targetFileIndex = header.IndexInMFT;
			}
			if (targetFileIndex < 0)
				return CreateFileResult.FileNotFound;

			// индекс файла мы получили. 
			// 1) проверить на то, что мы можем его изменять (а мы изменяем файл, раз меняем его имя)
			// 2) если все ок, переименовать и вернуть OK.

			if (!UserCanWriteFile(targetFileIndex, userId))
			{
				return CreateFileResult.NotEnoughRights;
			}

			RenameFileByMFTIndex(targetFileIndex, newName);

			return CreateFileResult.OK;
		}

		private void RenameFileByMFTIndex(int targetFileIndex, string newName)
		{
			var newNameBytes = newName.ToBytes().TrimOrExpandTo(MFTRecord.LengthOfFileName);

			var offset = GetMFTRecordOffsetByIndex(targetFileIndex);
			offset += MFTRecord.OffsetForFileName;

			stream.Position = offset;
			stream.Write(newNameBytes, 0, newNameBytes.Length);

		}

	}
}
