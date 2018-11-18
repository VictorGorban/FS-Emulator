using System;
using System.Collections.Generic;
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
        short UserId;
        Right Right;

        public UserRight( short userId, Right right)
        {
            UserId = userId;
            Right = right;
        }
    }
}
