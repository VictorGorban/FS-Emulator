using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
	public struct ServiceRecord
	{
		public static int SizeInBytes = 79;

		public const int OffsetForBlockSizeInBytes = 0;
		public const int OffsetForBlock_start_MFT = 4;
		public const int OffsetForBlock_start_Data = 8;
		public const int OffsetForMax_files_count = 12;
		public const int OffsetForMax_files_count_InsideDir = 16;
		public const int OffsetForMax_users_count = 20;
		public const int OffsetForFiles_count = 24;
		public const int OffsetForUsers_count = 28;
		public const int OffsetForNumber_Of_Blocks = 32;
		public const int OffsetForNumber_Of_Free_Blocks = 40;
		public const int OffsetForFS_Version = 48;
		public const int OffsetForVolume = 78;


		public int BlockSizeInBytes;
		public int Block_start_MFT;
		public int Block_start_Data;
		public int Max_files_count;
		public int Max_files_count_InsideDir;
		public int Max_users_count;
		public int Files_count;
		public int Users_count;
		public long Number_Of_Blocks;
		public long Number_Of_Free_Blocks;
		public byte[] FS_Version; //30
		public byte Volume;

		public ServiceRecord(int size_block, int block_start_MFT, int block_start_data, long number_Of_Blocks = 0, long number_Of_Free_Blocks = 0, string fS_Version = "Simple_NTFS v.1.0", char volume = 'A')
		{
			Max_files_count = 0;
			Max_files_count_InsideDir = 0;
			Max_users_count = 0;
			Files_count = 0;
			Users_count = 0;


			BlockSizeInBytes = size_block;
			Block_start_MFT = block_start_MFT;
			Block_start_Data = block_start_data;
			Number_Of_Blocks = number_Of_Blocks;
			Number_Of_Free_Blocks = number_Of_Free_Blocks;
			FS_Version = Encoding.ASCII.GetBytes(fS_Version) ?? throw new ArgumentNullException(nameof(fS_Version));
			if (FS_Version.Length != 30)
				FS_Version = FS_Version.TrimOrExpandTo(30);
			Volume = (byte)volume;
		}

		public byte[] ToBytes()
		{
			var list = new List<byte>();

			list.AddRange(BitConverter.GetBytes(BlockSizeInBytes));
			list.AddRange(BitConverter.GetBytes(Block_start_MFT));
			list.AddRange(BitConverter.GetBytes(Block_start_Data));
			list.AddRange(BitConverter.GetBytes(Max_files_count));
			list.AddRange(BitConverter.GetBytes(Max_files_count_InsideDir));
			list.AddRange(BitConverter.GetBytes(Max_users_count));
			list.AddRange(BitConverter.GetBytes(Files_count));
			list.AddRange(BitConverter.GetBytes(Users_count));
			list.AddRange(BitConverter.GetBytes(Number_Of_Blocks));
			list.AddRange(BitConverter.GetBytes(Number_Of_Free_Blocks));
			list.AddRange(FS_Version);
			list.Add(Volume);

			return list.ToArray();
		}

		public static ServiceRecord FromBytes(byte[] bytes)
		{
			var res = new ServiceRecord();

			byte[] buf;
			using (var ms = new MemoryStream(bytes))
			{
				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.BlockSizeInBytes = BitConverter.ToInt32(buf,0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Block_start_MFT = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Block_start_Data = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Max_files_count = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Max_files_count_InsideDir = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Max_users_count = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Files_count = BitConverter.ToInt32(buf, 0);

				buf = new byte[4];
				ms.Read(buf, 0, buf.Length);
				res.Users_count = BitConverter.ToInt32(buf, 0);

				buf = new byte[8];
				ms.Read(buf, 0, buf.Length);
				res.Number_Of_Blocks = BitConverter.ToInt64(buf, 0);

				buf = new byte[8];
				ms.Read(buf, 0, buf.Length);
				res.Number_Of_Free_Blocks = BitConverter.ToInt64(buf, 0);

				buf = new byte[30];
				ms.Read(buf, 0, buf.Length);
				res.FS_Version = buf;

				res.Volume = (byte)ms.ReadByte();
			}

			return res;
		}
	}
}
