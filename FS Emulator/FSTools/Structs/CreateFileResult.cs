using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	/// <summary>
	/// Ok - действие удалось. Остальные - действие не удалось.
	/// </summary>
	public enum CreateFileResult
	{
		OK,
		DirNotExists,
		/// <summary>
		/// Файл с таким именем уже существует в этой директории
		/// </summary>
		FileAlreadyExists,
		/// <summary>
		/// Недостаточно прав для редактирования директории, которой файл создается
		/// </summary>
		NotEnoughRights,
		NotEnoughSpace,
		/// <summary>
		/// Уже достигнуто макс. кол-во файлов (макс. кол-во записей в MFT)
		/// </summary>
		MaxFilesNumberReached,
		/// <summary>
		/// Нужно для переименования
		/// </summary>
		FileNotFound,
		InvalidFileName,
		PathTooLong
	}
}
