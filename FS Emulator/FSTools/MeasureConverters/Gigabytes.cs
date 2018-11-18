namespace FS_Emulator
{
    public static class Gigabytes
    {
        public static int FromBytes(int numberBytes)
        {
            return numberBytes / 1024 / 1024 / 1024;
        }

        public static int FromKilobytes(int numberKilobytes)
        {
            return numberKilobytes / 1024 / 1024;
        }

        public static int FromMegabytes(int numberMegabytes)
        {
            return numberMegabytes / 1024;
        }

        public static int FromGigabytes(int numberGigabytes)
        {
            return numberGigabytes;
        }

    }
}
