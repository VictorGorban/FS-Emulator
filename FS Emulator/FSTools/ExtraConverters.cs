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

        public static long ToLong(this DateTime dateTime)
        {
            return long.Parse("" + dateTime.Year + dateTime.Month + dateTime.Day + dateTime.Hour + dateTime.Minute + dateTime.Second);
        }

        public static DateTime ToDateTime(this long dateTimeLong)
        {
            var s = dateTimeLong.ToString();
            var year = int.Parse(s.Substring(0, 4));
            var month = int.Parse(s.Substring(4, 2));
            var day = int.Parse(s.Substring(6, 2));
            var hour = int.Parse(s.Substring(8, 2));
            var minute = int.Parse(s.Substring(10, 2));
            var second = int.Parse(s.Substring(12, 2));
            return new DateTime(year, month, day, hour, minute, second);
        }

		public static byte[] ToASCIIBytes(this string str, int requiredCountOfBytes)
		{
			return Encoding.ASCII.GetBytes(str).TrimOrExpandTo(requiredCountOfBytes);
		}
	}
}
