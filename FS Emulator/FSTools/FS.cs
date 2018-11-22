using FS_Emulator.FSTools.Structs;
using System;
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



		/* макс. кол-во MFT-записей. 
		 * Вычислить через <spaceForMFTZone> - (<spaceForUsers (всегда одинаковое)> + <spaceForList_Blocks>) 
		 * => (blocks)spaceForMFT 
		 * => / (bytes)size_Block 
		 * => (bytes) spaceForMFT / 1024 
		 * => maxFiles */




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
		private bool TestFSFile(string path, int blockSizeInBytes, int capacityInMegabytes, byte[] usersZoneBytes, byte[] list_BlocksBytes)
		{
			// открываю файл, считываю зоны через MFT (т.е. стандартная функция, которую еще надо сделать) и сравниваю байты с ToBytes() моих структур.

			// Зачем мне это нужно? Проверить считывание через MFT. И то, не забыл ли я чего.

			// Блин, а ведь Size-то подобавлять в MFT я и забыл. Самое время это исправить на паре. 
			// В алгоритме все в порядке (точно), надо Size присвоить после создания записи MFT. Все равно новая MFT запись 100% будет c Size=0. 
			// А такие особенности, как с этими service файлами, есть только с ними. Поэтому логично присваивать Size после создания MFT. Все, пора спать.

			return true;
		}

		private byte[] GetNewUsersZoneBytes()
		{
			var usersZoneBytes = new List<byte>();
			var firstUser = new UserRecord(0, "System", "system", "");
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
		private void FormatFSFile(string path, int blockSizeInBytes, int capacityInMegabytes)
		{
			/*Короч, я понял.
			 Вручную:
			 заполняю RootDir with MFT
			 MFT*/
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);
			var usersBytes = GetNewUsersZoneBytes();


			#region Заполнение данных rootDir
			var fileHeaders = new FileHeader[] {
				new FileHeader(1,"$MFT".ToASCIIBytes(50)),
				new FileHeader(3,"$Users".ToASCIIBytes(50)),
				new FileHeader(4,"$List_Blocks".ToASCIIBytes(50))
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

			var List_BlocksRecord = new MFTRecord(3, "$List_Blocks", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForList_BlocksRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#region Users


			var blockUsersZoneFrom = blockZoneOfList_BlocksTo;
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, UserRecord.SizeInBytes * MaxUsersCount);
			var blockUsersZoneTo = blockUsersZoneFrom + blocksCountForUsersZone;

			var dataForUsersRecordBytes = new List<byte>();
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneFrom));
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersZoneTo));

			var UsersRecord = new MFTRecord(4, "$Users", "$.", FileType.Bin, UserRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForUsersRecordBytes.ToArray(), isNotInMFT: true);

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

			var MftRecord = new MFTRecord(1, "$MFT", "$.", FileType.Bin, MFTRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForMFTRecordBytes.ToArray(), isNotInMFT: true);
			#endregion


			var RootDirRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, listHeadersBytes.ToArray());


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

			var _2ndMFTRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, listHeadersBytes.ToArray());
			_2ndMFTRecord.Index = 2;
			_2ndMFTRecord.IsFileExists = false;
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

			var serviceRecord = new ServiceRecord(blockSizeInBytes, blockMFTRecordFrom, blockDataFrom);
			serviceRecord.Max_files_count = maxFilesCount;
			serviceRecord.Files_count = 4;
			serviceRecord.Max_users_count = 64;
			serviceRecord.Files_count = 2; // system and admin

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

		private int GetBlockCountForBytes(int blockSizeInBytes, int bytesCount)
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

		private byte[] GetNewBlocksFreeOrUsedZoneBytes(int capacityInMegabytes, int clusterSizeInBytes)
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
		/// 
		/// </summary>
		/// <param name="path">путь к файлу</param>
		/// <param name="fileName">имя файла</param>
		/// <returns>MFTRecord</returns>
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

		public 

		public int GetMaxFilesCount()
		{
			stream.Position = 0 + ServiceRecord.OffsetForMax_files_count;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var maxFilesCount = BitConverter.ToInt32(buf, 0);
			return maxFilesCount;
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


		public int GetBlockSizeInBytes()
		{
			stream.Position = 0 + ServiceRecord.OffsetForBlockSizeInBytes;
			var buf = new byte[4];
			stream.Read(buf, 0, buf.Length);
			var blockSizeInBytes = BitConverter.ToInt32(buf, 0);
			return blockSizeInBytes;
		}

		/// <summary>
		/// Возвращает блок, где находится MFT
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
		/// Возвращает блок, где находится MFT
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

		public void CreateFile(long blockStart, long size/*in bytes*/, string name, string path, FileType fileType, int size_Unit = 1, byte[] data = null)
		{
			if (data == null)
				data = new byte[0];
			/*Создать заголовок, вставить его в директорию.
			 Создать MFTRecord, вставить ее в MFT
			 Заголовок нужен только для отображения в директории!*/


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

		public void ModifyFile(byte[] path, byte[] fileName, byte[] newData)
		{
			// Нда уж, вот тут придется подумать. Что, если файл полностью вмещается в его же пространство (и пространство после него?)
			/* А ведь это идеальная ситуация. 
			 * (Да хрен там, не будет такого. 
			 * Под файл выделился блок - и все. Остальные записываются сразу после него. Тупо, но проще. )
			 * */

		}

		//public void AppendDataToFile(byte[] path, byte[] fileName, byte[] newData)
		//{

		//}


	}
}
