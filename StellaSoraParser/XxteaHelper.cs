using System.Text;

namespace StellaSoraParser;

public sealed class XXTeaHelper
{
	private static uint MX(uint sum, uint y, uint z, int p, uint e, uint[] k)
	{
		return (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[(int)(checked((IntPtr)(unchecked((long)(p & 3) ^ (long)((ulong)e)))))] ^ z);
	}

	public static byte[] Decrypt(byte[] data, byte[] key)
	{
		if (data.Length == 0)
		{
			return data;
		}
		return ToByteArray(Decrypt(ToUInt32Array(data, false), ToUInt32Array(FixKey(key), false)), true);
	}

	public static byte[] Decrypt(byte[] data, string key)
	{
		return Decrypt(data, utf8.GetBytes(key));
	}

	public static byte[] DecryptBase64String(string data, byte[] key)
	{
		return Decrypt(Convert.FromBase64String(data), key);
	}

	public static byte[] DecryptBase64String(string data, string key)
	{
		return Decrypt(Convert.FromBase64String(data), key);
	}

	public static string DecryptToString(byte[] data, byte[] key)
	{
		return utf8.GetString(Decrypt(data, key));
	}

	public static string DecryptToString(byte[] data, string key)
	{
		return utf8.GetString(Decrypt(data, key));
	}

	public static string DecryptBase64StringToString(string data, byte[] key)
	{
		return utf8.GetString(DecryptBase64String(data, key));
	}

	public static string DecryptBase64StringToString(string data, string key)
	{
		return utf8.GetString(DecryptBase64String(data, key));
	}

	private static uint[] Decrypt(uint[] v, uint[] k)
	{
		int n = v.Length;
		if (n < 2)
		{
			return v;
		}
		int rounds = 6 + 52 / n;
		uint sum = (uint)(rounds * 2654435769U); // use unsigned multiplication directly

		uint y = v[0];
		uint z;
		uint e;
		while (sum != 0)
		{
			e = (sum >> 2) & 3;
			for (int p = n - 1; p > 0; p--)
			{
				z = v[p - 1];
				y = v[p] -= MX(sum, y, z, p, e, k);
			}
			z = v[n - 1];
			y = v[0] -= MX(sum, y, z, 0, e, k);
			sum -= 2654435769U;
		}

		return v;
	}

	private static byte[] FixKey(byte[] key)
	{
		if (key.Length == 16)
		{
			return key;
		}
		byte[] array = new byte[16];
		if (key.Length < 16)
		{
			key.CopyTo(array, 0);
		}
		else
		{
			Array.Copy(key, 0, array, 0, 16);
		}
		return array;
	}

	private static uint[] ToUInt32Array(byte[] data, bool includeLength)
	{
		int num = data.Length;
		int num2 = ((num & 3) == 0) ? (num >> 2) : ((num >> 2) + 1);
		uint[] array;
		if (includeLength)
		{
			array = new uint[num2 + 1];
			array[num2] = (uint)num;
		}
		else
		{
			array = new uint[num2];
		}
		for (int i = 0; i < num; i++)
		{
			array[i >> 2] |= (uint)((uint)data[i] << ((i & 3) << 3));
		}
		return array;
	}

	private static byte[] ToByteArray(uint[] data, bool includeLength)
	{
		int num = data.Length << 2;
		if (includeLength)
		{
			int num2 = (int)data[data.Length - 1];
			num -= 4;
			if (num2 < num - 3 || num2 > num)
			{
				return null;
			}
			num = num2;
		}
		byte[] array = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = (byte)(data[i >> 2] >> ((i & 3) << 3));
		}
		return array;
	}

	private static readonly UTF8Encoding utf8 = new UTF8Encoding();
}