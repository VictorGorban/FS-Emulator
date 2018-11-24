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
		FileAlreadyExists,
		NotEnoughRights,
		NotEnoughSpace,
		MaxFilesNumberReached
	}
}
