using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	class ShortFileInfo
	{
		public string FileName;
		public FileType FileType;
		public DateTime Time_Modification;
		public int FileSize;
		public string OwnerLogin;
		public string OwnerRights;


		public ShortFileInfo() { }

		public override string ToString()
		{
			return string.Format("{0,20} {1,5} {2,20} {3,10} {4,10} {5,7}", FileName.Replace("\0", ""), FileType.ToString(), Time_Modification.ToString("yyyy/MM/dd hh:mm:ss"), FileSize, OwnerLogin, OwnerRights);
		}
	}
}
 