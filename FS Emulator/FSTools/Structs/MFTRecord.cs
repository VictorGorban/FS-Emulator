using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public struct MFTRecord
	{
		public const int SpaceForData = 769;
		public const int SizeInBytes = 1024;

		public int Index;
		public bool IsFileExists;
		/// <summary>
		/// Где начинаются данные файла. Если -1, то файл полностью в MFT.
		/// </summary>
		public int Number_FirstBlock;
		public byte[] FileName; // длина - 50 симв.
		public byte[] Path; // 206 симв.
		public FileType FileType;
		public int DataUnitSize; /*Для текста - 1B, для MFT, например, 1024B*/
		public long Time_Creation;
		public long Time_Modification;
		public int Size;

		#region flags
		public bool IsSystem;
		public bool IsHidden;
		#endregion
		public UserRight[] User_Rights; // 64 - max
		public byte[] Data; // все, что останется от 1 КБ. 1024-237 = MFTRecord.SpaceForData

		public MFTRecord(int index, string fileName, string path, FileType fileType, int dataUnitSize, DateTime time_Creation, DateTime time_Modification, bool isSystem, bool isHidden, UserRight[] user_Rights, byte[] data = null, int number_FirstBlock = -1)
		// без / и           с / в конце. Если нет - добавлю.
		{
			Index = index;

			IsFileExists = true;
			Size = 0;
			Number_FirstBlock = number_FirstBlock;

			if (data != null)
			{
				// Надеюсь, я не додумаюсь СОЗДАТЬ файл с >MFTRecord.SpaceForData байт.
				if (data.Length > MFTRecord.SpaceForData)
					throw new ArgumentException("Слишком большая длина data для создания записи MFT", nameof(data));
				Data = data;
			}
			else
			{
				// to-do: расположить данные на "диске", раз уж них есть Number_FirstBlock.

				Data = new byte[MFTRecord.SpaceForData];
			}



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
			list.AddRange(BitConverter.GetBytes(Index));
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

		//to-do: FromBytes. Хотя бы для удобного теста.

	}
}
