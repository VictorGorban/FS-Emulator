using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
    public enum Right: byte
    {
        None,
        R,
        W,
        RW,
    }

    public struct UserRight
    {
		public const int SizeInBytes = 3;

        public short UserId;
        public Right Right;

        public UserRight( short userId, Right right)
        {
            UserId = userId;
            Right = right;
        }

		public byte[] ToBytes()
		{
			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(UserId));
			list.Add((byte)Right);

			return list.ToArray();
		}

		/*public static UserRight FromBytes(byte[] bytes)
		{
			if (bytes.Length != SizeInBytes)
				throw new ArgumentException("Число байт не верно.", nameof(bytes));
			var res = new UserRight();
			using (var ms = new MemoryStream(bytes))
			{
				// скобочки - для разграничения области видимости. Потому что мне каждый раз нужен новый буфер.
				{
					byte[] buffer = new byte[2];
					ms.Read(buffer, 0, buffer.Length);
					res.UserId = BitConverter.ToInt16(buffer, 0);
				}

				{
					byte[] buffer = new byte[1];
					ms.Read(buffer, 0, buffer.Length);
					res.Right = (Right)buffer[0];
				}
			}

			return res;
		}*/
    }
}
