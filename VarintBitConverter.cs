using System;

namespace Debris
{
	public class VarintBitConverter
	{
		public static byte[] GetVarintBytes(byte value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(short value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(ushort value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(int value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(uint value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(long value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte[] GetVarintBytes(ulong value)
		{
			var buffer = new byte[10];
			var pos = 0;
			do
			{
				var byteVal = value & 0x7f;
				value >>= 7;

				if (value != 0)
				{
					byteVal |= 0x80;
				}

				buffer[pos++] = (byte)byteVal;

			} while (value != 0);

			var result = new byte[pos];
			Buffer.BlockCopy(buffer, 0, result, 0, pos);

			return result;
		}
		public static byte ToByte(byte[] bytes)
		{
			return (byte)ToTarget(bytes, 8);
		}
		public static short ToInt16(byte[] bytes)
		{
			var value = ToTarget(bytes, 16);
			return (short)value;
		}
		public static ushort ToUInt16(byte[] bytes)
		{
			return (ushort)ToTarget(bytes, 16);
		}
		public static int ToInt32(byte[] bytes)
		{
			var value = ToTarget(bytes, 32);
			return (int)value;
		}
		public static uint ToUInt32(byte[] bytes)
		{
			return (uint)ToTarget(bytes, 32);
		}
		public static long ToInt64(byte[] bytes)
		{
			var value = ToTarget(bytes, 64);
			return (long)value;
		}
		public static ulong ToUInt64(byte[] bytes)
		{
			return ToTarget(bytes, 64);
		}
		private static ulong ToTarget(byte[] bytes, int sizeBites)
		{
			int shift = 0;
			ulong result = 0;
			foreach (ulong byteValue in bytes)
			{
				ulong tmp = byteValue & 0x7f;
				result |= tmp << shift;
				if (shift > sizeBites)
				{
					throw new ArgumentOutOfRangeException("bytes", "Byte array is too large.");
				}
				if ((byteValue & 0x80) != 0x80)
				{
					return result;
				}
				shift += 7;
			}
			throw new ArgumentException("Cannot decode varint from byte array.", "bytes");
		}
	}
}