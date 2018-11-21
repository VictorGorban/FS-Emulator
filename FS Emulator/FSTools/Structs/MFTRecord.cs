﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public partial struct MFTRecord
	{
		public const int SpaceForData = 544;
		public const int SizeInBytes = 1024;

		public const int OffsetForIndex = 0;
		public const int OffsetForIsFileExists = 4;
		public const int OffsetForIsNotInMFT = 5;

		public const int OffsetForFileName = 6;
		public const int OffsetForPath = 56;
		public const int OffsetForFileType = 262;
		public const int OffsetForDataUnitSize = 263;
		public const int OffsetForTime_Creation = 267;
		public const int OffsetForTime_Modification = 275;
		public const int OffsetForFileSize = 283;
		public const int OffsetForFlags = 287;
		public const int OffsetForUserRights = 288;
		public const int OffsetForData = 480;

		#region
		public int Index;
		public bool IsFileExists;
		/// <summary>
		/// Где начинаются данные файла. Если -1, то файл полностью в MFT.
		/// </summary>
		public bool IsNotInMFT;
		public byte[] FileName; // длина - 50 симв.
		public byte[] Path; // 206 симв.
		public FileType FileType;
		public int DataUnitSize; /*Для текста - 1B, для MFT, например, 1024B*/
		public long Time_Creation;
		public long Time_Modification;
		public int FileSize;

		#region flags
		public byte Flags; // [0] - IsUnfragmented, [3] - IsSystem, [4] - IsHidden		
		#endregion
		public UserRight[] User_Rights; // 64 - max
		public byte[] Data; // все, что останется от 1 КБ. 1024-237 = MFTRecord.SpaceForData

		public MFTRecord(int index, string fileName, string path, FileType fileType, int dataUnitSize, DateTime time_Creation, DateTime time_Modification, byte flags, UserRight[] user_Rights, byte[] data = null, bool isNotInMFT = false)
		// без / и           с / в конце. Если нет - добавлю.
		{
			Index = index;

			IsFileExists = true;
			FileSize = 0;
			IsNotInMFT = isNotInMFT;
			Flags = flags;

			if (data != null)
			{
				// Надеюсь, я не додумаюсь СОЗДАТЬ файл с >MFTRecord.SpaceForData байт.
				if (data.Length > MFTRecord.SpaceForData)
					throw new ArgumentException("Слишком большая длина data для создания записи MFT", nameof(data));
				Data = data.TrimOrExpandTo(SpaceForData);
			}
			else
			{

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
			if (Path.Length > 206)
				throw new ArgumentException("Упс, путь слишком длииинный", nameof(path));
			if (Path.Length < 206)
			{
				Path = Path.TrimOrExpandTo(206);
			}


			FileType = fileType;
			DataUnitSize = dataUnitSize;
			Time_Creation = time_Creation.ToLong();
			Time_Modification = time_Modification.ToLong();
			

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
		#endregion
		public byte[] ToBytes()
		{ // сейчас это toBytes без Data. Специально чтоб вычислить размер Data.

			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(Index));
			list.AddRange(BitConverter.GetBytes(IsFileExists));
			list.AddRange(BitConverter.GetBytes(IsNotInMFT));
			list.AddRange(FileName);
			list.AddRange(Path);
			list.Add((byte)FileType);
			list.AddRange(BitConverter.GetBytes(DataUnitSize));
			list.AddRange(BitConverter.GetBytes(Time_Creation));
			list.AddRange(BitConverter.GetBytes(Time_Modification));
			list.AddRange(BitConverter.GetBytes(FileSize));
			list.Add(Flags);
			#region Get Bytes For User Rights
			var bytesForUserRights = new List<byte>();
			foreach(var right in User_Rights)
			{
				bytesForUserRights.AddRange(right.ToBytes());
			}
			#endregion
			list.AddRange(bytesForUserRights.ToArray());
			list.AddRange(Data);


			return list.ToArray();
		}


	}
}
