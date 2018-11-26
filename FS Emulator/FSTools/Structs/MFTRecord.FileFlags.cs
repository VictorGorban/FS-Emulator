namespace FS_Emulator.FSTools.Structs
{
		public static class FileFlags
		{
			public const byte 
				None = 0,
				Unfragmented =				0b_10000000,
				System =					0b_00010000,
				Hidden =					0b_00001000,
				SystemHidden =				0b_00011000,
				UnfragmentedSystemHidden =	0b_10011000;
		}
	
}
