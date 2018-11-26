using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
    public struct UserRights
    {
		public const int SizeInBytes = 4;

		public const int OffsetForUserId = 0;
		public const int OffsetForRights = 2;

		public const short AllRights = 0b111_111;
		public const short OnlyMeRights = 0b111_000;

		public short UserId;
        public short Rights;

        public UserRights( short userId, short rights)
        {
            UserId = userId;
            Rights = rights;
        }


		public byte[] ToBytes()
		{
			var list = new List<byte>();
			list.AddRange(BitConverter.GetBytes(UserId));
			list.AddRange(BitConverter.GetBytes(Rights));

			return list.ToArray();
		}

		public static UserRights FromBytes(byte[] bytes)
		{
			if (bytes.Length != SizeInBytes)
				throw new ArgumentException("Число байт не верно.", nameof(bytes));
			var res = new UserRights();
			using (var ms = new MemoryStream(bytes))
			{
				// скобочки - для разграничения области видимости. Потому что мне каждый раз нужен новый буфер.
				{
					byte[] buffer = new byte[2];
					ms.Read(buffer, 0, buffer.Length);
					res.UserId = BitConverter.ToInt16(buffer, 0);
				}

				{
					byte[] buffer = new byte[2];
					ms.Read(buffer, 0, buffer.Length);
					res.Rights = BitConverter.ToInt16(buffer, 0);
				}
			}

			return res;
		}
    }
}
