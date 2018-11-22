using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
	public static class ExtraConverters
	{
		public static T[] TrimOrExpandTo<T>(this T[] array, int requiredLength)
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

		public static string ToNormalizedPath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (path.Length == 0)
			{
				throw new ArgumentException("путь не может быть пустым",nameof(path));
			}

			if (path.Last() != '/')
				path = path + '/';

			return path;
		}

		public static byte[] ToNormalizedPath(this byte[] path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (path.Length == 0)
			{
				throw new ArgumentException("путь не может быть пустым", nameof(path));
			}

			if (path.Last() != '/')
			{
				var list = path.ToList();
				list.Add((byte)'/');
				return list.ToArray();
			}//else

			return path;
		}

		public static byte[] ToBytes(this string str)
		{
			return Encoding.ASCII.GetBytes(str);
		}

		public static string ToASCIIString(this byte[] bytes)
		{
			return Encoding.ASCII.GetString(bytes);
		}
	}
}
