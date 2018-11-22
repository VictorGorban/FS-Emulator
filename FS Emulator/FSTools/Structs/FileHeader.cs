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
		public const int SizeInBytes = 54;

		public const int OffsetForNumberInMFT = 0;
		public const int OffsetForFileName = 4;


		public int NumberInMFT;
		public byte[] FileName;


		public FileHeader(int numberInMFT, byte[] fileName)
		{
			NumberInMFT = numberInMFT;
			FileName = fileName ?? throw new ArgumentNullException("Да как можно было отдать в FileHeader null на место имени файла? КАААК???", nameof(fileName));
			if (FileName.Length != 50)
				FileName = FileName.TrimOrExpandTo(50);
		}

		public byte[] ToBytes()
		{
			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(NumberInMFT));
			list.AddRange(FileName);

			return list.ToArray();
		}

		/*public static FileHeader FromBytes(byte[] bytes)
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
					res.NumberInMFT = BitConverter.ToInt32(buffer, 0);
				}

				{
					byte[] buffer = new byte[30];
					ms.Read(buffer, 0, buffer.Length);
					res.FileName = buffer;
				}
			}

			return res;
		}*/
	}
}
