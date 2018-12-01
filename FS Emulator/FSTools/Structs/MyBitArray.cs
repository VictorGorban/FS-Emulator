using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools.Structs
{
	public class MyBitArray:IEnumerable
	{
		bool[] bits = new bool[8];

		public MyBitArray(byte b)
		{ // &: true только если оба true
			if((b & 0b10_00_00_00) != 0)
				bits[0] = true;
			if ((b & 0b01_00_00_00) != 0)
				bits[1] = true;
			if ((b & 0b00_10_00_00) != 0)
				bits[2] = true;
			if ((b & 0b00_01_00_00) != 0)
				bits[3] = true;
			if ((b & 0b00_00_10_00) != 0)
				bits[4] = true;
			if ((b & 0b00_00_01_00) != 0)
				bits[5] = true;
			if ((b & 0b00_00_00_10) != 0)
				bits[6] = true;
			if ((b & 0b00_00_00_01) != 0)
				bits[7] = true;
		}

		public IEnumerator GetEnumerator()
		{
			return bits.GetEnumerator();
		}

		public bool this[int index]
		{
			get { return bits[index]; }
			set { bits[index] = value; }
		}

		public int Length => 8;

		public byte ToByte()
		{ // |: 1 сохранятся, 0 останется 0
			byte res = 0b00_00_00_00;
			if (bits[0] == true)
				res = (byte)(res | 0b10_00_00_00);
			if (bits[1] == true)
				res = (byte)(res | 0b01_00_00_00);
			if (bits[2] == true)
				res = (byte)(res | 0b00_10_00_00);
			if (bits[3] == true)
				res = (byte)(res | 0b00_01_00_00);
			if (bits[4] == true)
				res = (byte)(res | 0b00_00_10_00);
			if (bits[5] == true)
				res = (byte)(res | 0b00_00_01_00);
			if (bits[6] == true)
				res = (byte)(res | 0b00_00_00_10);
			if (bits[7] == true)
				res = (byte)(res | 0b00_00_00_01);


			return res;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
