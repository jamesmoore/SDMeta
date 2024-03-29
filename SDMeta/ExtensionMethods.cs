﻿using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SDMeta
{
	public static partial class ExtensionMethods
	{
		// Returns the human-readable file size for an arbitrary, 64-bit file size 
		// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
		public static string GetBytesReadable(this long i)
		{
			// Get absolute value
			long absolute_i = i < 0 ? -i : i;
			// Determine the suffix and readable value
			string suffix;
			double readable;
			if (absolute_i >= 0x1000000000000000) // Exabyte
			{
				suffix = "EB";
				readable = i >> 50;
			}
			else if (absolute_i >= 0x4000000000000) // Petabyte
			{
				suffix = "PB";
				readable = i >> 40;
			}
			else if (absolute_i >= 0x10000000000) // Terabyte
			{
				suffix = "TB";
				readable = i >> 30;
			}
			else if (absolute_i >= 0x40000000) // Gigabyte
			{
				suffix = "GB";
				readable = i >> 20;
			}
			else if (absolute_i >= 0x100000) // Megabyte
			{
				suffix = "MB";
				readable = i >> 10;
			}
			else if (absolute_i >= 0x400) // Kilobyte
			{
				suffix = "KB";
				readable = i;
			}
			else
			{
				return i.ToString("0 B"); // Byte
			}
			// Divide by 1024 to get fractional value
			readable /= 1024;
			// Return formatted number with suffix
			return readable.ToString("0.## ") + suffix;
		}

		public static string ComputeSHA256Hash(this string rawData)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

			StringBuilder builder = new();
			for (int i = 0; i < bytes.Length; i++)
			{
				builder.Append(bytes[i].ToString("x2"));
			}
			return builder.ToString();
		}

		[GeneratedRegex(@"\s+")]
		private static partial Regex WhitespaceRegex();

		public static string NormalizeString(this string stringToNormalize)
		{
			return WhitespaceRegex().Replace(stringToNormalize, " ").Trim().ToLower();
		}
	}
}
