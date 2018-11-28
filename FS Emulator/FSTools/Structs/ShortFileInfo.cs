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


			return string.Format("{0,50} {1,5} {2,10} {3,7} {4,20} {5,8}", FileName.Replace("\0", ""), FileType.ToString(), Time_Modification, FileSize, OwnerLogin, OwnerRights);
		}
	}
}
 