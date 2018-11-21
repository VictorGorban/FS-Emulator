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
		public static int SizeInBytes = 53;

		public int Size_blockInBytes;
		public int Block_start_MFT;
		public int Block_start_data;
		public long Number_Of_Blocks;
		public long Number_Of_Free_Blocks;
		public byte[] FS_Version; //30
		public byte Volume;

		public ServiceRecord(int size_block, int block_start_MFT, int block_start_data, long number_Of_Blocks =0, long number_Of_Free_Blocks =0, string fS_Version = "Simple_NTFS v.1.0", char volume = 'A')
		{
			Size_blockInBytes = size_block;
			Block_start_MFT = block_start_MFT;
			Block_start_data = block_start_data;
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
			list.AddRange(BitConverter.GetBytes(Size_blockInBytes));
			list.AddRange(BitConverter.GetBytes(Block_start_data));
			list.AddRange(BitConverter.GetBytes(Number_Of_Blocks));
			list.AddRange(BitConverter.GetBytes(Number_Of_Free_Blocks));
			list.AddRange(FS_Version);
			list.Add(Volume);

			return list.ToArray();

		}

		public static ServiceRecord FromBytes(byte[] bytes)
		{
			if (bytes.Length != SizeInBytes)
				throw new ArgumentException("Число байт не верно.", nameof(bytes));
			var res = new ServiceRecord();
			using (var ms = new MemoryStream(bytes))
			{
				// скобочки - для разграничения области видимости. Потому что мне каждый раз нужен новый буфер.
				{
					byte[] buffer = new byte[4];
					ms.Read(buffer, 0, buffer.Length);
					res.Size_blockInBytes = BitConverter.ToInt32(buffer, 0);

				}
				{
					byte[] buffer = new byte[4];
					ms.Read(buffer, 0, buffer.Length);
					res.Block_start_MFT = BitConverter.ToInt32(buffer, 0);

				}
				{
					byte[] buffer = new byte[4];
					ms.Read(buffer, 0, buffer.Length);
					res.Block_start_data = BitConverter.ToInt32(buffer, 0);

				}

				{
					byte[] buffer = new byte[8];
					ms.Read(buffer, 0, buffer.Length);
					res.Number_Of_Blocks = BitConverter.ToInt16(buffer, 0);

				}

				{
					byte[] buffer = new byte[8];
					ms.Read(buffer, 0, buffer.Length);
					res.Number_Of_Free_Blocks = BitConverter.ToInt16(buffer, 0);

				}

				{
					byte[] buffer = new byte[30];
					ms.Read(buffer, 0, buffer.Length);
					res.FS_Version = buffer;
				}

				{
					byte[] buffer = new byte[1];
					ms.Read(buffer, 0, buffer.Length);
					res.Volume = buffer[0];
				}
			}

			return res;
		}
	}
}
