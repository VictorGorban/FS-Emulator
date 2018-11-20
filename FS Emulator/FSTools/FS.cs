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



		public static FileStream Create(string path, int capacityInMegabytes, int blockSizeInBytes)
		{
			// создать пространство для BlocksFreeOrBusy
			var list_BlocksBytes = GetNewBlocksFreeOrUsedZoneInBytes(capacityInMegabytes, blockSizeInBytes);
			int blocksCountForZoneOfFreeOrUsedBlocks = GetBlockCountForBytes(blockSizeInBytes, list_BlocksBytes.Length);


			// создать Service
			var serviceZoneBytes = GetNewServiceZoneInBytes(blockSizeInBytes);
			int blocksCountForServiceZone = GetBlockCountForBytes(blockSizeInBytes, serviceZoneBytes.Length);


			// создать Users
			var usersZoneBytes = GetNewUsersZoneBytes();
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, usersZoneBytes.Length);


			// создать MFT
			var MFTZoneBytes = GetNewMFTZoneBytes(blockSizeInBytes, capacityInMegabytes, serviceZoneBytes, usersZoneBytes, list_BlocksBytes);
			int blocksCountForMFTZone = GetBlockCountForBytes(list_BlocksBytes.Length / 10, blockSizeInBytes);
			/*Теперь, когда у нас есть MFT с RootDir, надо сделать
			 AlterFile(Service), AlterFile(Users)*/




			FileStream file;
			using (file = File.Create(path, blockSizeInBytes))
			{
				file.SetLength(Bytes.FromMegabytes(capacityInMegabytes));
				//Записываю файлы, но учитываю смещение.

				file.Write(serviceZoneBytes, 0, serviceZoneBytes.Length);


			}

			return file;
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

		private static byte[] GetNewMFTZoneBytes(int blockSizeInBytes, int capacityInMegabytes, byte[] serviceZoneBytes, byte[] usersBytes, byte[] list_BlocksBytes)
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
			var MFTBytes = listMFTBytes.ToArray();
			#endregion

			// Так, теперь надо записать весь этот кошмар на "диск".
			// Сначала List_Blocks, потом Users, и наконец MFT. Начальные блоки у меня есть, вперед. 

			var usersZoneBytes = GetNewUsersZoneBytes();
			// такое имя из-за того, что раньше в этом методе я уже это имя юзал.
			var list_BlocksBlocksBytes = GetNewBlocksFreeOrUsedZoneInBytes(capacityInMegabytes, blockSizeInBytes);


			// to-do: у меня все номера блоков, куда писать. Теперь мне просто надо записать туда эти данные. Вручную, да
			// 1) List_Blocks
			// 2) Users
			// 3) MFT


			/*Камон, да я ж делал это уже!!! 
			 * Просто Position устанавливать, да и все. Всего 3 раза. 3!!!*/










			// все, что дальше - Obsolete. Я использую этот метод как CreateNewMFTZone.
			var listBytes = new List<byte>();

			return listBytes.ToArray();
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
			List<byte> serviceZoneByteList;
			{
				var serviceZoneData = new
				{
					Size_block = (short)blockSizeInBytes,
					Block_start_data = (int)0,
					Number_Of_Blocks = (long)0,
					Number_Of_Free_Blocks = (long)0,
					FS_Version = "Simple_NTFS v.1.0",
					Volume = 'A'
				};

				// FS_Version_OK_UTF8Array - это 30-bytes FS_Version
				char[] FS_Version_OK_UTF8Array;
				{
					// надо к FS_Version добавить недостающие char
					var extraChars = new char[FSNameSize - serviceZoneData.FS_Version.Length]; // 30-17 = 13

					var temp = serviceZoneData.FS_Version.ToList();
					temp.AddRange(extraChars);
					FS_Version_OK_UTF8Array = temp.ToArray();
				}


				var serviceZone = new
				{
					size_block = BitConverter.GetBytes(serviceZoneData.Size_block),
					block_start_data = BitConverter.GetBytes(serviceZoneData.Block_start_data),
					number_of_blocks = BitConverter.GetBytes(serviceZoneData.Number_Of_Blocks),
					number_of_free_blocks = BitConverter.GetBytes(serviceZoneData.Number_Of_Free_Blocks),
					fs_v_ASCII_Bytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, Encoding.UTF8.GetBytes(FS_Version_OK_UTF8Array)),
					VolumeASCII = BitConverter.GetBytes(serviceZoneData.Volume)
				};


				serviceZoneByteList = new List<byte>();
				serviceZoneByteList.AddRange(BitConverter.GetBytes(serviceZoneData.Size_block));
				serviceZoneByteList.AddRange(BitConverter.GetBytes(serviceZoneData.Block_start_data));
				serviceZoneByteList.AddRange(BitConverter.GetBytes(serviceZoneData.Number_Of_Blocks));
				serviceZoneByteList.AddRange(BitConverter.GetBytes(serviceZoneData.Number_Of_Free_Blocks));
				serviceZoneByteList.AddRange(serviceZone.fs_v_ASCII_Bytes);
				serviceZoneByteList.AddRange(serviceZone.VolumeASCII);
			}

			return serviceZoneByteList.ToArray();
		}

		private static byte[] GetNewBlocksFreeOrUsedZoneInBytes(int capacityInMegabytes, int clusterSizeInBytes)
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
