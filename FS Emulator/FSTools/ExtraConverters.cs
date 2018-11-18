using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
    public static class ExtraConverters
    {
        public static T[] TrimOrExpandTo <T> (this T[] array, int requiredLength)
        {
            var list = array.ToList();
            if (list.Count < requiredLength)
            {
                list.AddRange(new T[requiredLength - list.Count]);
            }
            else if (list.Count > requiredLength)
            {
                list.RemoveRange(requiredLength, list.Count - requiredLength);
            }
            return list.ToArray();
        }
    }
}
