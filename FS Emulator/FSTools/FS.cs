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


		public static FileStream Open(string path)
		{
			return null;
		}



		public static FileStream Create(string path, int capacityInMegabytes, int blockSizeInBytes)
		{
			// создать пространство для BlocksFreeOrBusy
			var ZoneOfFreeOrUsedBlocksBytes = GetNewBlocksFreeOrUsedZoneInBytes(capacityInMegabytes, blockSizeInBytes);
			int blocksCountForZoneOfFreeOrUsedBlocks = GetBlockCountForBytes(blockSizeInBytes, ZoneOfFreeOrUsedBlocksBytes.Length);
			// to-do: закончить переименовв


			// создать Service
			var serviceZoneBytes = GetNewServiceZoneInBytes(blockSizeInBytes);
			int blocksCountForServiceZone = GetBlockCountForBytes(blockSizeInBytes, serviceZoneBytes.Length);


			// создать Users
			var usersZoneBytes = GetNewUsersZoneBytes();
			int blocksCountForUsersZone = GetBlockCountForBytes(blockSizeInBytes, usersZoneBytes.Length);


			// создать MFT
			var MFTZoneBytes = GetNewMFTZoneBytes();
			int blocksCountForMFTZone = GetBlockCountForBytes(ZoneOfFreeOrUsedBlocksBytes.Length / 10, blockSizeInBytes);

			/*to-do:
			 1) Create MFT file
			 2) Create other files
			 3) Create root dir (root/)
			 Я все равно не хочу делать папку для MFT файлов. Ну их.*/



			// "вручную" заполнить MFT (RegisterFile(BlockStart, Size) вместо CreateFile)
			// наверное, лучше так:
			// 1) создать все пустое
			// 2) Зарегать файлы (типа SystemTools.RegisterDataAsFile(string name, long blockStart, long lengthInBlocks))


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

		private static byte[] GetNewMFTZoneBytes()
		{
			var firstMftRecord = new MFTRecord("$MFT", "", FileType.Bin, 1024, DateTime.Now, DateTime.Now, true, true, new[] { new UserRight(0, Right.RW) });


			return firstMftRecord.ToBytes();
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



		public static void CreateFile(long blockStart, long size/*in bytes*/, string name, string where/*path*/, FileType fileType, int size_Unit = 1, byte[] data = null)
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

	}
}
