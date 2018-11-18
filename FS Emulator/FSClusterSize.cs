namespace FS_Emulator
{
    /// <summary>
    /// Не изменяйте порядок. На него завязан индекс в form.
    /// Не изменяйте названия (Если изменяете, то измените его же в FSClusterSize_Corresponds)
    /// </summary>
    public enum FSClusterSize
    {
        _512B,
        _1KB,
        _2KB,
        _4KB,
        _8KB
    }

    public enum FSClusterSize_CorrespondsBytes
    {
        _512B = 512,
        _1KB = 1024,
        _2KB = 2048,
        _4KB = 4096,
        _8KB = 8192
    }
}
