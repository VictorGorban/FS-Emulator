namespace FS_Emulator
{
    public static class Bytes
    {// все надо проверить. 
     // Обновить. Я перепутал Bytes с Kilobytes.
     // Добавить Bits
     //И протестить.

        public static int FromBits(int numberBits)
        {
            return numberBits / 8;
        }

        public static int FromBytes(int numberBytes)
        {
            return numberBytes;
        }

        public static int FromKilobytes(int numberKilobytes)
        {
            return numberKilobytes * 1024;
        }

        public static int FromMegabytes(int numberMegabytes)
        {
            return numberMegabytes * 1024 * 1024;
        }

        public static int FromGigabytes(int numberBytes)
        {
            return numberBytes * 1024 * 1024 * 1024;
        }
    }

    public static class Kilobytes
    {
        public static int FromBits(int numberBits)
        {
            return numberBits / 8 / 1024;
        }

        public static int FromBytes(int numberBytes)
        {
            return numberBytes / 1024;
        }

        public static int FromKilobytes(int numberKilobytes)
        {
            return numberKilobytes;
        }

        public static int FromMegabytes(int numberMegabytes)
        {
            return numberMegabytes * 1024;
        }

        public static int FromGigabytes(int numberBytes)
        {
            return numberBytes * 1024 * 1024;
        }
    }
}
