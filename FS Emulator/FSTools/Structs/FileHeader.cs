using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public struct FileHeader
	{
		public const int SizeInBytes = 8;

		public const int OffsetForIndexInMFT = 0;
		public const int OffsetForParentDirindexInMFT = 4;


		public int IndexInMFT; // если 0, то это или rootDir, или пустое место.
		public int ParentDirIndexInMFT;

		public FileHeader(int indexInMFT, int parentDirIndexInMFT)
		{
			IndexInMFT = indexInMFT;
			ParentDirIndexInMFT = parentDirIndexInMFT;
		}

		public byte[] ToBytes()
		{
			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(IndexInMFT));
			list.AddRange(BitConverter.GetBytes(ParentDirIndexInMFT));

			return list.ToArray();
		}

		public static FileHeader FromBytes(byte[] bytes)
		{
			if (bytes.Length != SizeInBytes)
				throw new ArgumentException("Число байт не верно.", nameof(bytes));
			var res = new FileHeader();
			using (var ms = new MemoryStream(bytes))
			{
				// скобочки - для разграничения области видимости. Потому что мне каждый раз нужен новый буфер.
				{
					byte[] buffer = new byte[4];
					ms.Read(buffer, 0, buffer.Length);
					res.IndexInMFT = BitConverter.ToInt32(buffer, 0);
				}

				{
					byte[] buffer = new byte[4];
					ms.Read(buffer, 0, buffer.Length);
					res.ParentDirIndexInMFT = BitConverter.ToInt32(buffer, 0);
				}
			}

			return res;
		}
	}
}
