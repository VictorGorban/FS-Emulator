using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
    [Serializable]
    internal struct UserRecord
    {
        public short User_id;
        public byte[] Name;
        public byte[] Login;
        public byte[] PasswordHash;


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
            using(var sha = System.Security.Cryptography.SHA512.Create())
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

    }
}
