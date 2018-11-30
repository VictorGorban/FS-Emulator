namespace FS_Emulator.FSTools
{
	public enum CreateUserResult
	{
		OK,
		MaxUsersCountReached,
		UserAlreadyExists,
		/// <summary>
		/// При создании пользователя, я еще создаю ему папку. Папка может быть не создана (maxNumberOfFiles, notEnoughRights)
		/// </summary>
		OKButCanNotCreateUserDir
	}
}