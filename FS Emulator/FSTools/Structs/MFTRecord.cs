using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public struct MFTRecord
	{
		private bool IsFileExists;
		private byte[] FileName; // длина - 50 симв.
		private byte[] Path; // 206 симв.
		private FileType FileType;
		int DataUnitSize; /*Для текста - 1B, для MFT, например, 1024B*/
		private long Time_Creation;
		private long Time_Modification;
		private int Size;
		
		#region flags
		private bool IsSystem;
		private bool IsHidden;
		#endregion
		private UserRight[] User_Rights; // 64 - max
		private byte[] Data; // все, что останется от 1 КБ. 1024-237 = 787

		public MFTRecord(string fileName, string path, FileType fileType, int dataUnitSize, DateTime time_Creation, DateTime time_Modification, bool isSystem, bool isHidden, UserRight[] user_Rights)
		// без / и           с / в конце. Если нет - добавлю.
		{
			IsFileExists = true;
			Size = 0;
			Data = new byte[787];

			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (user_Rights == null)
				throw new ArgumentNullException(nameof(user_Rights));
			if (dataUnitSize == 0)
			{
				throw new ArgumentException("Упс, размер единицы данных не может быть 0 (иначе будет бесконечное чтение)", nameof(dataUnitSize));
			}

			FileName = Encoding.ASCII.GetBytes(fileName);
			if (FileName.Length != 50)
				FileName = FileName.TrimOrExpandTo(50);

			if (path != "")
			{
				if (path.Last() != '/')
					path = path + '/';
			}
			
			Path = Encoding.ASCII.GetBytes(path);
			if (FileName.Length > 206)
				throw new ArgumentException("Упс, путь слишком длииинный", nameof(path));
			if (FileName.Length < 206)
			{
				FileName = FileName.TrimOrExpandTo(206);
			}


			FileType = fileType;
			DataUnitSize = dataUnitSize;
			Time_Creation = time_Creation.ToLong();
			Time_Modification = time_Modification.ToLong();
			IsSystem = isSystem;
			IsHidden = isHidden;

			User_Rights = user_Rights;
			if (User_Rights.Length > 64)
			{
				throw new ArgumentException("Сильно много прав пользователей", nameof(user_Rights));
			}
			if (User_Rights.Length < 64)
			{
				User_Rights = User_Rights.TrimOrExpandTo(64);
			}
		}

		public byte[] ToBytes()
		{ // сейчас это toBytes без Data. Специально чтоб вычислить размер Data.

			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(IsFileExists));
			list.AddRange(FileName);
			list.AddRange(Path);
			list.Add((byte)FileType);
			list.AddRange(BitConverter.GetBytes(DataUnitSize));
			list.AddRange(BitConverter.GetBytes(Time_Creation));
			list.AddRange(BitConverter.GetBytes(Time_Modification));
			list.AddRange(BitConverter.GetBytes(Size));
			list.AddRange(BitConverter.GetBytes(IsSystem));
			list.AddRange(BitConverter.GetBytes(IsHidden));
			list.AddRange(Data);


			return list.ToArray();
		}


	}
}
