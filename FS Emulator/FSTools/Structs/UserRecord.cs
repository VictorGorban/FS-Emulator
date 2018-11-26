using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	[Serializable]
	public struct UserRecord
	{
		public const int SizeInBytes = 126;

		public const int OffsetForUser_id = 0;
		public const int OffsetForName = 2;
		public const int OffsetForLogin = 32;
		public const int OffsetForPasswordHash = 62;

		public const int NameLength = 30;
		public const int LoginLength = 30;
		public const int PasswordHashLength = 64;

		public short User_id;
		// не может быть "" при нормальных условиях. "" - если не существует или не создан.
		public byte[] Name; // 30
		public byte[] Login; // 30
		public byte[] PasswordHash; // 64


		public UserRecord(short user_id, string name, string login, string password)
		{
			User_id = user_id;
			Name = Encoding.ASCII.GetBytes(name) ?? throw new ArgumentNullException(nameof(name));
			if (Name.Length != 30)
				Name = Name.TrimOrExpandTo(30);

			Login = Encoding.ASCII.GetBytes(login) ?? throw new ArgumentNullException(nameof(login));
			if (Login.Length != 30)
				Login = Login.TrimOrExpandTo(30);

			if (password == null)
				throw new ArgumentNullException(nameof(password));
			using (var sha = System.Security.Cryptography.SHA512.Create())
			{
				var buffer = Encoding.ASCII.GetBytes(password);
				PasswordHash = sha.ComputeHash(buffer);
			}

		}
		public byte[] ToBytes()
		{
			var bytes = new List<byte>();
			bytes.AddRange(BitConverter.GetBytes(User_id));
			bytes.AddRange(Name);
			bytes.AddRange(Login);
			bytes.AddRange(PasswordHash);

			return bytes.ToArray();
		}

		public static UserRecord FromBytes(byte[] bytes)
		{
			if (bytes.Length != SizeInBytes)
				throw new ArgumentException("Число байт не верно.", nameof(bytes));
			var res = new UserRecord();
			using (var ms = new MemoryStream(bytes))
			{
				// скобочки - для разграничения области видимости. Потому что мне каждый раз нужен новый буфер.
				{
					byte[] buffer = new byte[2];
					ms.Read(buffer, 0, buffer.Length);
					res.User_id = BitConverter.ToInt16(buffer, 0);
				}
				{
					byte[] buffer = new byte[30];
					ms.Read(buffer, 0, buffer.Length);
					res.Name = buffer;
				}
				{
					byte[] buffer = new byte[30];
					ms.Read(buffer, 0, buffer.Length);
					res.Login = buffer;
				}
				{
					byte[] buffer = new byte[64];
					ms.Read(buffer, 0, buffer.Length);
					res.PasswordHash = buffer;
				}

			}

			return res;
		}
	}
}
