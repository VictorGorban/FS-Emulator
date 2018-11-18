using FS_Emulator.FSTools.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
    public class FS
    {
        public static int FSNameSize = 30;


        public static FileStream Open(string path)
        {
            return null;
        }



        public static FileStream Create(string path, int capacityInMegabytes, int blockSizeInBytes)
        {
            // создать пространство для BlocksFreeOrBusy
            var zoneFreeOrUsedBlocks = GetNewBlocksFreeOrUsedZoneInBytes(capacityInMegabytes, blockSizeInBytes);
            int blocksNumberForZone0 = GetBlockCountForBytes(blockSizeInBytes, zoneFreeOrUsedBlocks.Length);
            // to-do: закончить переименование


            // создать Service
            var zone1Bytes = GetNewServiceZoneInBytes(blockSizeInBytes);


            // создать Users
            var zone3Bytes = GetNewUsersZoneBytes();


            // создать MFT


            // "вручную" заполнить MFT (RegisterFile(BlockStart, Size) вместо CreateFile)
            // наверное, лучше так:
            // 1) создать все пустое
            // 2) Зарегать файлы (типа SystemTools.RegisterDataAsFile(string name, long blockStart, long lengthInBlocks))


            FileStream file;
            using (file = File.Create(path, blockSizeInBytes))
            {
                file.SetLength(Bytes.FromMegabytes(capacityInMegabytes));
                file.Write(zone1Bytes, 0, 0);
            }

            return file;
        }

        private static byte[] GetNewUsersZoneBytes()
        {
            var usersZoneBytes = new List<byte>();
            var firstUser = new User(0, "System", "system", "");
            var secondUser = new User(1, "Admin", "admin", "admin");

            var bytes = new List<byte>();
            bytes.AddRange(firstUser.ToBytes());
            bytes.AddRange(secondUser.ToBytes());

            return bytes.ToArray();
        }

        private static int GetBlockCountForBytes(int blockSizeInBytes, int bytesCount)
        {
            var res = bytesCount / blockSizeInBytes;
            var temp = bytesCount % blockSizeInBytes;
            
            if(temp == 0)
            {
                return res;
            }
            else
            {
                return res + 1;
            }
        }

        private static byte[] GetNewServiceZoneInBytes(int clusterSizeInBytes)
        {
            List<byte> zone1ByteList;
            {
                var zone1Data = new
                {
                    Size_block = (short)clusterSizeInBytes,
                    Block_start_data = (int)0,
                    Number_Of_Blocks = (long)0,
                    Number_Of_Free_Blocks = (long)0,
                    FS_Version = "Simple_NTFS v.1.0"
                };

                // FS_Version_OK_UTF8Array - это 30-bytes FS_Version
                char[] FS_Version_OK_UTF8Array;
                {
                    // надо к FS_Version добавить недостающие char
                    var extraChars = new char[FSNameSize - zone1Data.FS_Version.Length]; // 30-17 = 13

                    var temp = zone1Data.FS_Version.ToList();
                    temp.AddRange(extraChars);
                    FS_Version_OK_UTF8Array = temp.ToArray();
                }


                var zone1 = new
                {
                    size_block = BitConverter.GetBytes(zone1Data.Size_block),
                    block_start_data = BitConverter.GetBytes(zone1Data.Block_start_data),
                    number_of_blocks = BitConverter.GetBytes(zone1Data.Number_Of_Blocks),
                    number_of_free_blocks = BitConverter.GetBytes(zone1Data.Number_Of_Free_Blocks),
                    fs_v_ASCII_Bytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, Encoding.UTF8.GetBytes(FS_Version_OK_UTF8Array))

                };


                zone1ByteList = new List<byte>();
                zone1ByteList.AddRange(BitConverter.GetBytes(zone1Data.Size_block));
                zone1ByteList.AddRange(BitConverter.GetBytes(zone1Data.Block_start_data));
                zone1ByteList.AddRange(BitConverter.GetBytes(zone1Data.Number_Of_Blocks));
                zone1ByteList.AddRange(BitConverter.GetBytes(zone1Data.Number_Of_Free_Blocks));
                zone1ByteList.AddRange(zone1.fs_v_ASCII_Bytes);
            }

            return zone1ByteList.ToArray();
        }

        private static byte[] GetNewBlocksFreeOrUsedZoneInBytes(int capacityInMegabytes, int clusterSizeInBytes)
        {
            var numberOfBlocks = Bytes.FromMegabytes(capacityInMegabytes) / clusterSizeInBytes;

            var bits = new System.Collections.BitArray(numberOfBlocks);
            byte[] bytes;
            
            if (numberOfBlocks % 8 == 0)
            {
                bytes = new byte[bits.Length / 8];
            }
            else
            {
                bytes = new byte[bits.Length / 8 + 1];
            }

            bits.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
