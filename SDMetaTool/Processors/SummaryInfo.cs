using BetterConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDMetaTool.Processors
{
	internal class SummaryInfo : IPngFileListProcessor
	{
		public void ProcessPngFiles(IEnumerable<PngFile> tracks, string root)
		{
			var distinctPrompts = tracks.Select(p => p.Parameters?.PromptHash).Distinct().ToList();
			var distinctFullPrompts = tracks.Select(p => new
			{
				p.Parameters?.PromptHash,
				p.Parameters?.NegativePromptHash
			}).Distinct().ToList();

			Console.WriteLine($"{tracks.Count()} png files");
			Console.WriteLine($"{GetBytesReadable(tracks.Sum(p => p.Length))} stored");
			Console.WriteLine($"{distinctPrompts.Count} positive prompts");
			Console.WriteLine($"{distinctFullPrompts.Count} positive/negative prompts");

			var modelGroups = tracks.
				GroupBy(p => p.Parameters?.ModelHash).
				Select(p => new { ModelHash = p.Key, Count = p.Count(), Models = p.Select(q => q.Parameters?.Model ?? "<empty>").Distinct() }).ToList().OrderByDescending(p => p.Count);


			var headers = new[]
			{
				new ColumnHeader("Hash"),
				new ColumnHeader("Count", Alignment.Right),
				new ColumnHeader("Model name(s)"),
			};

			var table = new Table(new TableConfiguration() { 
				wrapText = true,
				hasInnerRows= false,
				hasInnerColumns= false,
				textWrapLimit=50
			}, headers);

			foreach (var modelGroup in modelGroups)
			{
				table.AddRow(modelGroup.ModelHash ?? "<empty>", modelGroup.Count, string.Join(", ", modelGroup.Models.ToArray()));
			}
			Console.Write(table.ToString());
		}

		// Returns the human-readable file size for an arbitrary, 64-bit file size 
		// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
		public string GetBytesReadable(long i)
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
			readable = readable / 1024;
			// Return formatted number with suffix
			return readable.ToString("0.## ") + suffix;
		}
	}
}
