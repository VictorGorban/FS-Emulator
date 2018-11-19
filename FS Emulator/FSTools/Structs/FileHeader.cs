using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public struct FileHeader
	{
		public const int SizeInBytes = 54;


		public int NumberInMFT;
		public byte[] FileName;


		public FileHeader(int numberInMFT, byte[] fileName)
		{
			NumberInMFT = numberInMFT;
			FileName = fileName ?? throw new ArgumentNullException("Да как можно было отдать в FileHeader null на место имени файла? КАААК???",nameof(fileName));
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
	}
}
