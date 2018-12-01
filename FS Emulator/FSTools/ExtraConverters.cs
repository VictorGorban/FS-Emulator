using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS_Emulator.FSTools.Structs;

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
			return long.Parse("" + dateTime.Year + dateTime.Month.ToString("d2") + dateTime.Day.ToString("d2") + dateTime.Hour.ToString("d2") + dateTime.Minute.ToString("d2") + dateTime.Second.ToString("d2"));
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

		public static string ToNormalizedPath(this string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (path.Length == 0)
			{
				return path;
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
				return "".ToBytes(); // Если это директория корня. Она пустая.
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
			var str = Encoding.ASCII.GetString(bytes).Replace("\0","");

			return str;
		}

		public static string ToRightsString(this short rights)
		{
			string result = "";
			if (FS.OwnerCanRead(rights))
				result += "R";
			else
				result += "-";
			if (FS.OwnerCanWrite(rights))
				result += "W";
			else
				result += "-";
			result += "_";
			if (FS.OthersCanRead(rights))
				result += "R";
			else
				result += "-";
			if (FS.OthersCanWrite(rights))
				result += "W";
			else
				result += "-";

			return result;
		}

		public static short GetRightsFromString(string str)
		{
			if (str == null || str.Length < 5)
				throw new ArgumentException("Строка недостаточной длины");

			str = str.ToUpper();
			short result = UserRights.NoneRights;

			if (str[0] == 'R')
				result = (short)(result | UserRights.OwnerCanReadRights);
			if (str[1] == 'W')
				result = (short)(result | UserRights.OwnerCanWriteRights);

			if (str[3] == 'R')
				result = (short)(result | UserRights.OthersCanReadRights);
			if (str[4] == 'W')
				result = (short)(result | UserRights.OthersCanWriteRights);

			return result;
		}
		
	}
}
