namespace FS_Emulator.FSTools.Structs
{
	/// <summary>
	/// Ok - действие удалось. Остальные - действие не удалось.
	/// </summary>
	public enum RemoveFileResult
	{
		OK,
		DirNotExists,
		NotEnoughRights
	}
}
