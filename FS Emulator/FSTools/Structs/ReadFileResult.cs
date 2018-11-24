namespace FS_Emulator.FSTools.Structs
{
	/// <summary>
	/// Ok - действие удалось. Остальные - действие не удалось.
	/// </summary>
	public enum ReadFileResult
	{
		OK,
		DirNotExists,
		FileNotExists,
		NotEnoughRights
	}
}
