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
		public static int MaxNumber_Users = 64;
		public static int MaxFiles = 0;
		public static int FilesCount = 0;
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
			// создать Service
			// to-do: создать структуру.
			var serviceZoneBytes = GetNewServiceZoneInBytes(blockSizeInBytes);

			// создать BlocksFreeOrBusy
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);
			int blocksCountForZoneOfFreeOrUsedBlocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);

			// создать Users
			var usersZoneBytes = GetNewUsersZoneBytes();
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, usersZoneBytes.Length);


			try
			{
				FormatFSFile(path, blockSizeInBytes, capacityInMegabytes, serviceZoneBytes, usersZoneBytes, list_BlocksBytes);


				if (!TestFSFile(path, blockSizeInBytes, capacityInMegabytes, serviceZoneBytes, usersZoneBytes, list_BlocksBytes)) System.Windows.Forms.MessageBox.Show("Кошмааар!!!");;

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

		private static void FormatFSFile(string path, int blockSizeInBytes, int capacityInMegabytes, byte[] serviceZoneBytes, byte[] usersBytes, byte[] list_BlocksBytes)
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
			var RootDirRecord = new MFTRecord(0, "$./", "", FileType.Dir, FileHeader.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, listHeadersBytes.ToArray());
			var ServiceRecord = new MFTRecord(2, "$Service", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.SystemHidden, new[] { new UserRight(0, Right.RW) }, serviceZoneBytes);

			#region List_blocks
			var blocksCountForZoneOfList_Blocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);

			var dataForList_BlocksRecordBytes = new List<byte>();
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(0));
			dataForList_BlocksRecordBytes.AddRange(BitConverter.GetBytes(blocksCountForZoneOfList_Blocks));

			var List_BlocksRecord = new MFTRecord(3, "$List_Blocks", "$.", FileType.Bin, 1, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForList_BlocksRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#region Users

			var blockUsersRecordFrom = blocksCountForZoneOfList_Blocks;
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, UserRecord.SizeInBytes*MaxNumber_Users);
			var blockUsersRecordTo = blockUsersRecordFrom + blocksCountForUsersZone;

			var dataForUsersRecordBytes = new List<byte>();
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersRecordFrom));
			dataForUsersRecordBytes.AddRange(BitConverter.GetBytes(blockUsersRecordTo));

			var UsersRecord = new MFTRecord(4, "$List_Blocks", "$.", FileType.Bin, UserRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForUsersRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#region MFT
			var blockMFTRecordFrom = blockUsersRecordTo;
			var bytesForMFTRecords = Bytes.FromKilobytes(5);
			var blockMFTRecordTo = bytesForMFTRecords / blockSizeInBytes;
			if (bytesForMFTRecords % blockSizeInBytes != 0)
				blockMFTRecordTo += 1;

			var dataForMFTRecordBytes = new List<byte>();
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordFrom));
			dataForMFTRecordBytes.AddRange(BitConverter.GetBytes(blockMFTRecordTo));

			var MftRecord = new MFTRecord(1, "$MFT", "$.", FileType.Bin, MFTRecord.SizeInBytes, DateTime.Now, DateTime.Now, FileFlags.UnfragmentedSystemHidden, new[] { new UserRight(0, Right.RW) }, dataForMFTRecordBytes.ToArray(), isNotInMFT: true);
			#endregion

			#endregion

			#region Создание исходных данных в MFT
			var listMFTBytes = new List<byte>();
			listMFTBytes.AddRange(RootDirRecord.ToBytes());
			listMFTBytes.AddRange(MftRecord.ToBytes());
			listMFTBytes.AddRange(ServiceRecord.ToBytes());
			listMFTBytes.AddRange(List_BlocksRecord.ToBytes());
			listMFTBytes.AddRange(UsersRecord.ToBytes());
			var MFTRecordsBytes = listMFTBytes.ToArray();
			#endregion

			// Так, теперь надо записать весь этот кошмар на "диск".
			// Сначала List_Blocks, потом Users, и наконец MFT. Начальные блоки у меня есть, вперед. 
			var list_BlocksRecordsBytes = GetNewBlocksFreeOrUsedZoneBytes(capacityInMegabytes, blockSizeInBytes);

			var usersRecordsBytes = GetNewUsersZoneBytes();



			// to-do: у меня все номера блоков, куда писать. Теперь мне просто надо записать туда эти данные. Вручную, да
			// 1) List_Blocks
			// 2) Users
			// 3) MFT
			var List_BlocksOffsetInBytes = 0;
			var UsersOffsetInBytes = blockUsersRecordFrom * blockSizeInBytes;
			var MFTOffsetInBytes = blockMFTRecordFrom * blockSizeInBytes;

			/*Камон, да я ж делал это уже!!! 
			 * Просто Position устанавливать, да и все. Всего 3 раза. 3!!!*/


			FileStream file;
			using (file = File.Create(path))
			{
				//Записываю файлы, но учитываю смещение.

				file.Position = List_BlocksOffsetInBytes;

				file.Write(list_BlocksRecordsBytes, 0, list_BlocksRecordsBytes.Length);
				file.Position = UsersOffsetInBytes;

				file.Write(usersRecordsBytes, 0, usersRecordsBytes.Length);
				file.Position = MFTOffsetInBytes;

				file.Write(MFTRecordsBytes, 0, MFTRecordsBytes.Length);
			}

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

		private static byte[] GetNewServiceZoneInBytes(int blockSizeInBytes)
		{
			var ServiceRecord = new ServiceRecord((short)blockSizeInBytes, 0);

			return ServiceRecord.ToBytes();
		}

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
