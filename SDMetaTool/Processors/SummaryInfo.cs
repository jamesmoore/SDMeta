﻿using BetterConsoleTables;
using SDMeta;
using SDMeta.Processors;
using System;
using System.Linq;

namespace SDMetaTool.Processors
{
    internal class SummaryInfo(IImageDir imageDir, IFileLister fileLister, IPngFileLoader pngFileLoader) : IPngFileListProcessor
	{
		public void ProcessPngFiles()
		{
			var fileNames = imageDir.GetPath().Select(fileLister.GetList).SelectMany(p => p).Distinct().ToList();
			var pngFiles = fileNames.Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).OrderBy(p => p.FileName).ToList();

			var distinctPrompts = pngFiles.Select(p => p.Parameters?.PromptHash).Distinct().ToList();
			var distinctFullPrompts = pngFiles.Select(p => new
			{
				p.Parameters?.PromptHash,
				p.Parameters?.NegativePromptHash
			}).Distinct().ToList();

			Console.WriteLine($"{pngFiles.Count} png files");
			Console.WriteLine($"{pngFiles.Sum(p => p.Length).GetBytesReadable()} stored");
			Console.WriteLine($"{distinctPrompts.Count} positive prompts");
			Console.WriteLine($"{distinctFullPrompts.Count} positive/negative prompts");

			var modelGroups = pngFiles.
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


	}
}
