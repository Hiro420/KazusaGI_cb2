using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public static class Utils
{
	public static uint AbilityHash(string str)
	{
		uint hash = 0;
		char[] asCharArray = str.ToCharArray();
		for (int i = 0; i < str.Length; i++)
		{
			hash = ((asCharArray[i] + 131 * hash) & 0xFFFFFFFF) >> 0;
		}
		return hash;
	}
	public static ulong GetPathHash(string path)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(path);
		byte[] array = new byte[(path.Length >> 8) + 1 << 8];
		Array.Copy(bytes, 0, array, 0, bytes.Length);
		using (MD5 md5 = MD5.Create())
		{
			byte[] hash = md5.ComputeHash(array);
			ulong num = 0UL;
			for (int i = 4; i >= 0; i--)
			{
				num <<= 8;
				num |= (ulong)hash[i];
			}
			return num;
		}
	}
	public static ulong PreLastToHash(byte Pre, uint Last) => ((ulong)Last << 8) | Pre;
}