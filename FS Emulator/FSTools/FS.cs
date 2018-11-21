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
		public static int FSNameSize = 30;
		public static int MaxUsersCount = 64;
		public static int MaxFilesCount;
		public static int FilesCount;
		public static int BlockStartMFT;
		public static int BlockSizeInBytes;
		/* макс. кол-во MFT-записей. 
		 * Вычислить через <spaceForMFTZone> - (<spaceForUsers (всегда одинаковое)> + <spaceForList_Blocks>) 
		 * => (blocks)spaceForMFT 
		 * => / (bytes)size_Block 
		 * => (bytes) spaceForMFT / 1024 
		 * => maxFiles */




		public static FileStream Open(string path)
		{
			return null;
		}



		public static void FormatOrCreate(string path, int capacityInMegabytes, int blockSizeInBytes)
		{
			BlockSizeInBytes = blockSizeInBytes;

			// создать BlocksFreeOrBusy
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);
			int blocksCountForZoneOfFreeOrUsedBlocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);

			// создать Users
			var usersZoneBytes = GetNewUsersZoneBytes();
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, usersZoneBytes.Length);


			try
			{
				FormatFSFile(path, blockSizeInBytes, capacityInMegabytes, usersZoneBytes, list_BlocksBytes);


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
		private static bool TestFSFile(string path, int blockSizeInBytes, int capacityInMegabytes, byte[] serviceZoneBytes, byte[] usersZoneBytes, byte[] list_BlocksBytes)
		{
			// открываю файл, считываю зоны через MFT (т.е. стандартная функция, которую еще надо сделать) и сравниваю байты с ToBytes() моих структур.

			// Зачем мне это нужно? Проверить считывание через MFT. И то, не забыл ли я чего.

			// Блин, а ведь Size-то подобавлять в MFT я и забыл. Самое время это исправить на паре. 
			// В алгоритме все в порядке (точно), надо Size присвоить после создания записи MFT. Все равно новая MFT запись 100% будет c Size=0. 
			// А такие особенности, как с этими service файлами, есть только с ними. Поэтому логично присваивать Size после создания MFT. Все, пора спать.

			return true;
		}

		private static byte[] GetNewUsersZoneBytes()
		{
			var usersZoneBytes = new List<byte>();
			var firstUser = new UserRecord(0, "System", "system", "");
			var secondUser = new UserRecord(1, "Admin", "admin", "admin");

			var bytes = new List<byte>();
			bytes.AddRange(firstUser.ToBytes());
			bytes.AddRange(secondUser.ToBytes());

			return bytes.ToArray();
		}

		private static void FormatFSFile(string path, int blockSizeInBytes, int capacityInMegabytes, byte[] usersBytes, byte[] list_BlocksBytes)
		{
			/*Короч, я понял.
			 Вручную:
			 заполняю RootDir with MFT
			 MFT*/
			#region Заполнение данных rootDir
			var fileHeaders = new FileHeader[] {
				new FileHeader(1,"$MFT".ToASCIIBytes(50)),
				new FileHeader(2,"$Service".ToASCIIBytes(50)),
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
			
			#region List_blocks
			var blocksCountForZoneOfList_Blocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);

			var dataForList_BlocksRecordBytes = new List<byte>();
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(0));
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blocksCountForZoneOfList_Blocks));

			var List_BlocksRecord = new MFTRecord(3, "$List_Blocks", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForList_BlocksRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#region Users

			var blockUsersRecordFrom = blocksCountForZoneOfList_Blocks;
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, UserRecord.SizeInBytes*MaxUsersCount);
			var blockUsersRecordTo = blockUsersRecordFrom + blocksCountForUsersZone;

			var dataForUsersRecordBytes = new List<byte>();
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersRecordFrom));
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersRecordTo));

			var UsersRecord = new MFTRecord(4, "$List_Blocks", "$.", FileType.Bin, UserRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForUsersRecordBytes.ToArray(), isNotInMFT: true);
			
			#endregion

			#region MFT


			var blockCountForFullMFTZone = list_BlocksBytes.Length / 100 * 12; //12% всех блоков - на MFT зону.
			var blockDataFrom = blockCountForFullMFTZone;
			var blocksForMFT = blockCountForFullMFTZone - (blocksCountForUsersZone + blocksCountForZoneOfList_Blocks);
			var bytesForMFT = blocksForMFT * blockSizeInBytes;
			var maxFiles = bytesForMFT / MFTRecord.SizeInBytes;
			MaxFilesCount = maxFiles;

			var blockMFTRecordFrom = blockUsersRecordTo;
			BlockStartMFT = blockMFTRecordFrom;
			var blockMFTRecordTo = blockDataFrom;

			var dataForMFTRecordBytes = new List<byte>();
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordFrom));
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordTo));

			var MftRecord = new MFTRecord(1, "$MFT", "$.", FileType.Bin, MFTRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForMFTRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			var serviceZoneBytes = new ServiceRecord(blockSizeInBytes, blockMFTRecordFrom, blockDataFrom).ToBytes();
			var ServiceRecord = new MFTRecord(2, "$Service", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, serviceZoneBytes);

			var RootDirRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, listHeadersBytes.ToArray());


			#endregion

			#region Создание исходных данных для MFT
			var listMFTBytes = new List<byte>();
			listMFTBytes.AddRange(RootDirRecord.ToBytes());
			listMFTBytes.AddRange(MftRecord.ToBytes());
			listMFTBytes.AddRange(ServiceRecord.ToBytes());
			listMFTBytes.AddRange(List_BlocksRecord.ToBytes());
			listMFTBytes.AddRange(UsersRecord.ToBytes());
			var MFTRecordsBytes = listMFTBytes.ToArray();
			#endregion


			var list_BlocksRecordsBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);

			var usersRecordsBytes = GetNewUsersZoneBytes();

			#region Заполнение size в записях MFT
			RootDirRecord.FileSize = listHeadersBytes.Count;
			ServiceRecord.FileSize = FSTools.ServiceRecord.SizeInBytes;
			List_BlocksRecord.FileSize = list_BlocksRecordsBytes.Length;
			UsersRecord.FileSize = usersRecordsBytes.Length;
			MftRecord.FileSize = MFTRecordsBytes.Length;
			#endregion

			#region Запись на "диск"
			var List_BlocksOffsetInBytes = 0;
			var UsersOffsetInBytes = blockUsersRecordFrom * blockSizeInBytes;
			var MFTOffsetInBytes = blockMFTRecordFrom * blockSizeInBytes;

			using (var file = File.Create(path))
			{
				file.Position = List_BlocksOffsetInBytes;

				file.Write(list_BlocksRecordsBytes, 0, list_BlocksRecordsBytes.Length);
				file.Position = UsersOffsetInBytes;

				file.Write(usersRecordsBytes, 0, usersRecordsBytes.Length);
				file.Position = MFTOffsetInBytes;

				file.Write(MFTRecordsBytes, 0, MFTRecordsBytes.Length);
			}

			#endregion
			FilesCount = 5;

		}

		private static int GetBlockCountForBytes(int blockSizeInBytes, int bytesCount)
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

		/*private static byte[] GetNewServiceZoneInBytes(int blockSizeInBytes)
		{
			// Можно и так, но тогда придется отдельно устанавливать некоторые поля, типа BlockCount (опа, забыл )
			return ServiceRecord.ToBytes();
		}*/

		private static byte[] GetNewBlocksFreeOrUsedZoneBytes(int capacityInMegabytes, int clusterSizeInBytes)
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



		public static bool IsFileExists(byte[] path, byte[] fileName)
		{
			


			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path">путь к файлу</param>
		/// <param name="fileName">имя файла</param>
		/// <returns>MFTRecord</returns>
		public static byte[] FindMFTRecord(byte[] path, byte[] fileName)
		{
			var byteStartMFT = BlockStartMFT * BlockSizeInBytes;



			return null; // такой не найден
		}

		public static byte[] FindFile(int mftIndex)
		{
			throw new NotImplementedException();
		}

		public static void CreateFile(long blockStart, long size/*in bytes*/, string name, string path, FileType fileType, int size_Unit = 1, byte[] data = null)
		{
			if (data == null)
				data = new byte[0];
			/*Создать заголовок, вставить его в директорию.
			 Создать MFTRecord, вставить ее в MFT
			 Заголовок нужен только для отображения в директории!*/

			/*Т.е. для этих MFT файлов я могу пропустить фазу заголовка.*/

		}

		public static void RemoveFile()
		{

		}

		public static void MoveFile()
		{

		}

		public static void RenameFile()
		{

		}

		public static void ModifyFile(byte[] path, byte[] fileName, byte[] newData)
		{
			// Нда уж, вот тут придется подумать. Что, если файл полностью вмещается в его же пространство (и пространство после него?)
			/* А ведь это идеальная ситуация. 
			 * (Да хрен там, не будет такого. 
			 * Под файл выделился блок - и все. Остальные записываются сразу после него. Тупо, но проще. )
			 * */

		}

		//public static void AppendDataToFile(byte[] path, byte[] fileName, byte[] newData)
		//{

		//}



	}
}
