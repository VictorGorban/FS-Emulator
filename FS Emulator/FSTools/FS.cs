﻿using FS_Emulator.FSTools.Structs;
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
	public class FS
	{
		public int FSNameSize = 30;
		public int MaxUsersCount = 64;
		public int MaxFilesCount;
		public int FilesCount;
		public Stream stream;
		public int CurrentUserId;
		public const int RootId = 0;
		public const int UsersFileIndex = 4;
		public const int offsetForServiceZone = 0;



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

				//if (!TestFSFile(path, blockSizeInBytes, capacityInMegabytes, serviceZoneBytes, usersZoneBytes, list_BlocksBytes)) System.Windows.Forms.MessageBox.Show("Кошмааар!!!");;

			}
			catch (IOException)
			{
				System.Windows.Forms.MessageBox.Show("Не удается создать или записать файл.");

			}

		}
		/// <summary>
		/// Нужен только для теста.
		/// </summary>
		public bool TestFSFile(string path, int blockSizeInBytes, int capacityInMegabytes, byte[] usersZoneBytes, byte[] list_BlocksBytes)
		{
			// открываю файл, считываю зоны через MFT (т.е. стандартная функция, которую еще надо сделать) и сравниваю байты с ToBytes() моих структур.

			// Зачем мне это нужно? Проверить считывание через MFT. И то, не забыл ли я чего.

			// Блин, а ведь Size-то подобавлять в MFT я и забыл. Самое время это исправить на паре. 
			// В алгоритме все в порядке (точно), надо Size присвоить после создания записи MFT. Все равно новая MFT запись 100% будет c Size=0. 
			// А такие особенности, как с этими service файлами, есть только с ними. Поэтому логично присваивать Size после создания MFT. Все, пора спать.

			return true;
		}

		public byte[] GetNewUsersZoneBytes()
		{
			var usersZoneBytes = new List<byte>();
			var firstUser = new UserRecord(0, "Root", "root", "");
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


			#region Заполнение данных rootDir
			var fileHeaders = new FileHeader[] {
				new FileHeader(1,0),
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
			var blocksCountForServiceZone = 1; // никак больше не получится. Она там весит 60Б
			var blockServiceZoneFrom = 0;
			var blockServiceZoneTo = blockServiceZoneFrom + blocksCountForServiceZone;

			#region List_blocks
			var blocksCountForList_BlocksZone = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);
			var blockZoneOfList_BlocksFrom = blockServiceZoneTo;
			var blockZoneOfList_BlocksTo = blockZoneOfList_BlocksFrom + blocksCountForList_BlocksZone;

			var dataForList_BlocksRecordBytes = new List<byte>();
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blockZoneOfList_BlocksFrom));
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blockZoneOfList_BlocksTo));

			var List_BlocksRecord = new MFTRecord(3, "$List_Blocks", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, 77), dataForList_BlocksRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#region Users


			var blockUsersZoneFrom = blockZoneOfList_BlocksTo;
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, UserRecord.SizeInBytes * MaxUsersCount);
			var blockUsersZoneTo = blockUsersZoneFrom + blocksCountForUsersZone;

			var dataForUsersRecordBytes = new List<byte>();
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneFrom));
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneTo));

			var UsersRecord = new MFTRecord(4, "$Users", "$.", FileType.Bin, UserRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, 77), dataForUsersRecordBytes.ToArray(), isNotInMFT: true);

			#endregion

			#region MFT


			var blockCountForFullMFTZone = list_BlocksBytes.Length / 100 * 12; //12% всех блоков - на MFT зону.
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

			var MftRecord = new MFTRecord(1, "$MFT", "$.", FileType.Bin, MFTRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new UserRights(0, 77), dataForMFTRecordBytes.ToArray(), isNotInMFT: true);
			#endregion


			var RootDirRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new UserRights(0, 77), listHeadersBytes.ToArray());


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

			var _2ndMFTRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new UserRights(0, 77), listHeadersBytes.ToArray())
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
				Max_users_count = 64
			};
			serviceRecord.Users_count = 2; // root and admin

			var serviceBytes = serviceRecord.ToBytes();


			stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			{
				stream.Position = ServiceZoneOffsetInBytes;
				stream.Write(serviceBytes, 0, serviceBytes.Length);

				stream.Position = List_BlocksOffsetInBytes;
				stream.Write(list_BlocksRecordsBytes, 0, list_BlocksRecordsBytes.Length);

				stream.Position = UsersOffsetInBytes;
				stream.Write(usersRecordsBytes, 0, usersRecordsBytes.Length);

				stream.Position = MFTOffsetInBytes;
				stream.Write(MFTRecordsBytes, 0, MFTRecordsBytes.Length);
			}

			#endregion
			FilesCount = 4;

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

			var bits = new System.Collections.BitArray(numberOfBlocks);
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

			if (GetIndexOfFile(parentDirIndex, fileName) >= 0)
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

			int indexOfNewFile = AddNewMFTRecord(fileName, GetFullFilePathByMFTRecord(parentDirRecord), GetFullFilePathByMFTRecord(parentDirRecord), fileType, size_Unit, userId, DateTime.Now, DateTime.Now, FileFlags.None, new UserRights((short)userId, 77));

			// индекс в MFT получили, теперь добавить его в директорию
			var addToDirResult = AddRecordToDir(parentDirIndex, indexOfNewFile);
			if (addToDirResult == ModifyFileResult.NotEnoughSpace)
			{
				return CreateFileResult.NotEnoughSpace;
			}

			// Все, добавили запись в директорию
			// добавляем запись в MFT


			return CreateFileResult.OK;
		}

		/// <summary>
		/// Создает запись MFT и возвращает ее индекс. Нет проверок на "можно ли добавить запись"
		/// </summary>
		/// <param name="fileName"></param>

		private int AddNewMFTRecord(string fileName, string path, FileType fileType, int unitSize, DateTime timeCreation, DateTime timeModification, byte flags, UserRights userRights)
		{
			
			path = path.ToNormalizedPath();

			
			int mftStart = GetByteStartMFT();
			int startPosition = mftStart;
			stream.Position = startPosition;
			byte[] checkedRecord = new byte[MFTRecord.SizeInBytes];
			do
			{
				stream.Read(checkedRecord, 0, checkedRecord.Length);
			} while (GetIsFileExists(checkedRecord));
			//нашел место. Теперь записать сюда данные. Индекс, бла-бла-бла. Индекс в любом случае можно взять из Position.

			stream.Position -= MFTRecord.SizeInBytes;
			#region Создание записи
			int recIndex = ((int)stream.Position - startPosition) / MFTRecord.SizeInBytes;
			byte[] newRecord = new MFTRecord(recIndex, fileName, path, fileType, unitSize, timeCreation, timeModification, flags, userRights).ToBytes();
			#endregion
			stream.Write(newRecord, 0, newRecord.Length);

			// И не забыть еще увеличить FileCount в Service.
			return IncreaseFileCount();
		}
		/// <summary>
		/// Увеличивает значение кол-ва файлов в системе на указанное кол-во.
		/// </summary>
		/// <param name="count">Число, на которое нужно увеличить</param>
		/// <returns>Новое кол-во файлов</returns>
		private int IncreaseFileCount(int count = 1)
		{
			int startPosition = offsetForServiceZone + ServiceRecord.OffsetForFiles_count;
			
			// считывание FilesCount
			stream.Position = startPosition;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			int oldfCount = BitConverter.ToInt32(buf,0);

			var newfCount = oldfCount + 1;
			byte[] newfCountBytes = BitConverter.GetBytes(newfCount);
			
			// Запись нового FilesCount
			stream.Position = startPosition;
			stream.Write(newfCountBytes,0,newfCountBytes.Length);

			return newfCount;
		}

		/// <summary>
		/// Уменьшает значение кол-ва файлов в системе на указанное кол-во.
		/// </summary>
		/// <param name="count">Число, на которое нужно уменьшить</param>
		/// <returns>Новое кол-во файлов</returns>
		private int DecreaseFileCount(int count = 1)
		{
			int startPosition = offsetForServiceZone + ServiceRecord.OffsetForFiles_count;

			// считывание FilesCount
			stream.Position = startPosition;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			int oldfCount = BitConverter.ToInt32(buf, 0);

			var newfCount = oldfCount - 1;
			byte[] newfCountBytes = BitConverter.GetBytes(newfCount);

			// Запись нового FilesCount
			stream.Position = startPosition;
			stream.Write(newfCountBytes, 0, newfCountBytes.Length);

			return newfCount;
		}

		private string GetFullFilePathByMFTRecord(byte[] record)
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
		}

		/// <summary>
		/// Пытается добавить новую запись в директорию с указанным индексом в MFT. Для этого нужен адрес этой записи, потому что надо пойти на диск и изменить данные.
		/// </summary>
		/// <param name="mftIndex">Индекс файла-директории в MFT</param>
		/// <param name="fileName">Имя создаваемого файла</param>
		/// <param name="userId">Индекс пользователя, от имени которого происходит добавление</param>
		/// <returns>Результат, удалось ли добавить запись.</returns>
		public ModifyFileResult AddRecordToDir(int mftIndex, int userId)
		{
			// Идем по Data. Если находим запись с Index==0, то заменяем ее, возвращаем OK.
			// Что ж, мы дошли до конца данных (fileSize). Если (MFTRecord.SpaceForData - fileSize) < FileHeader.SizeInBytes, то return NotEnoughSpace. Иначе без проблем записываем в конец.
			int dirSize = GetFileSize(mftIndex);

			if (MFTRecord.SpaceForData - dirSize < FileHeader.SizeInBytes)
				return ModifyFileResult.NotEnoughSpace;

			stream.Position = GetByteStartMFT() + mftIndex * MFTRecord.SizeInBytes;
			var startPosition = stream.Position + MFTRecord.OffsetForData;
			var endPosition = startPosition + dirSize;

			#region Собственно процесс поиска и добавления
			stream.Position = startPosition;

			while (stream.Position < endPosition)
			{
				var buf = new byte[FileHeader.SizeInBytes];
				stream.Read(buf, 0, buf.Length);
				var fheader = FileHeader.FromBytes(buf);
				if (fheader.IndexInMFT == 0) // Такого не может быть, т.к. 0 - это rootDir.
				{
					stream.Position -= FileHeader.SizeInBytes;
					,b
				}

			}
			// Ok, adding to end of the dir
			#endregion

			return ModifyFileResult.OK;
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
			if (mftRecord.Length < MFTRecord.SizeInBytes)
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

			if (userId != RootId) // Hi, I'm a regular user
			{
				if (ownerId == userId) // Hi, and I'm owner
				{
					if (!OwnerCanWrite(ownerRights.Rights))
						return false;
				}
				// другой юзер
				if (!OthersCanWrite(ownerRights.Rights)) // Hi, I'm a stranger, can I write in there?
					return false;

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

			if (userId != RootId) // Hi, I'm a regular user
			{
				if (ownerId == userId) // Hi, I'm owner
				{
					if (!OwnerCanRead(ownerRights.Rights))
						return false;
				}
				// другой юзер
				if (!OthersCanRead(ownerRights.Rights)) // Hi, I'm a stranger, can I write in there?
					return false;

			}
			// I AM ROOOT!!!
			return true;
		}

		public bool OwnerCanWrite(short rights)
		{
			var bools = new BitArray(rights);
			return bools[1] == true;
		}
		public bool OthersCanWrite(short rights)
		{
			var bools = new BitArray(rights);
			return bools[4] == true;
		}

		public bool OwnerCanRead(short rights)
		{
			var bools = new BitArray(rights);
			return bools[0] == true;
		}
		public bool OthersCanRead(short rights)
		{
			var bools = new BitArray(rights);
			return bools[0] == true;
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

		/// <summary>
		/// Возвращает индекс файла в MFT по его имени в директории
		/// </summary>
		/// <param name="indexOfDir">номер в MFT, под которым зарегистрирована директория</param>
		/// <param name="fileName">имя файла, который ищем</param>
		/// <returns>номер файла в MFT или -1, если не найден</returns>
		public int GetIndexOfFile(int indexOfDir, string fileName)
		{
			var fileNameBytes = fileName.ToBytes().TrimOrExpandTo(50);

			var mftDirRec = GetMFTRecordByIndex(indexOfDir);
			if (mftDirRec == null)
				return -1;
			var dirData = GetFileDataByMFTRecord(mftDirRec);

			using (var headersStream = new MemoryStream(dirData))
			{
				while (headersStream.Position < dirData.Length)
				{
					var buf = new byte[FileHeader.SizeInBytes];
					headersStream.Read(buf, 0, buf.Length);
					var header = FileHeader.FromBytes(buf);

					var recBytes = GetMFTRecordByIndex(header.IndexInMFT);
					var recNameBytes = new byte[50];
					using (var recMS = new MemoryStream(recNameBytes))
					{
						recMS.Position = MFTRecord.OffsetForFileName;
						recMS.Read(recNameBytes, 0, recNameBytes.Length);
					}

					if (recNameBytes.SequenceEqual(fileNameBytes))
						return header.IndexInMFT;
				}
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
				var bytes = new byte[4];
				int block = 0;
				do
				{
					recStream.Position = MFTRecord.OffsetForData;
					recStream.Read(bytes, 0, bytes.Length);
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
			return CreateFile(parentDirIndex, dirName, FileType.Dir, userId, FileHeader.SizeInBytes);
		}

		public void RemoveFile()
		{

		}

		public void MoveFile()
		{

		}

		public void RenameFile()
		{

		}

		public ModifyFileResult ModifyFile(int mftIndex, byte[] newData)
		{
			// Нда уж, вот тут придется подумать. Что, если файл полностью вмещается в его же пространство (и пространство после него?)
			/* А ведь это идеальная ситуация. 
			 * (Да хрен там, не будет такого. 
			 * Под файл выделился блок - и все. Остальные записываются сразу после него. Тупо, но проще. )
			 * */


			return ModifyFileResult.OK;
		}

		//public void AppendDataToFile(byte[] path, byte[] fileName, byte[] newData)
		//{

		//}


	}
}
