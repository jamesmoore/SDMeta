using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SDMetaTool.Processors
{
	class CSVPngFileLister : IPngFileListProcessor
	{
		private readonly IDirectoryProcessor directoryProcessor;
		private readonly IPngFileLoader pngFileLoader;
		private readonly string outfile;
		private readonly bool distinct;

		public CSVPngFileLister(IDirectoryProcessor directoryProcessor, IPngFileLoader pngFileLoader, string outfile, bool distinct)
		{
			this.directoryProcessor = directoryProcessor;
			this.pngFileLoader = pngFileLoader;
			this.outfile = outfile;
			this.distinct = distinct;
		}

		public void ProcessPngFiles(string root)
		{
			using var writer = new StreamWriter(outfile);
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

			var fileNames = directoryProcessor.GetList(root);
			var pngFiles = fileNames.Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).OrderBy(p => p.Filename).ToList();

			var csvs = distinct ? GetCSVDistinct(pngFiles) : GetCSVPerItem(pngFiles);
			csv.WriteRecords(csvs);
		}

		private static IEnumerable<CSVEntry> GetCSVPerItem(IEnumerable<PngFile> tracks)
		{
			return tracks.OrderBy(p => p.LastUpdated).Select(p => ToCSV(p, 1));
		}

		private IEnumerable<CSVEntry> GetCSVDistinct(IEnumerable<PngFile> tracks)
		{
			var groupedBy = tracks.GroupBy(p => new
			{
				p.Parameters?.PromptHash,
				p.Parameters?.NegativePromptHash
			});

			var tracks2 = groupedBy.Select(p => ToCSV(p.OrderBy(p => p.LastUpdated).First(), p.Count())).OrderBy(p => p.LastUpdated);
			return tracks2;
		}

		private static CSVEntry ToCSV(PngFile p, int count)
		{
			var generationParams = p.Parameters ?? new GenerationParams();
			return new CSVEntry()
			{
				Filename = p.Filename,
				LastUpdated = p.LastUpdated,
				Length = p.Length,
				Prompt = generationParams.Prompt,
				NegativePrompt = generationParams.NegativePrompt,
				Parameters = generationParams.Params,
				Warnings = generationParams.Warnings,
				Count = count,
				ModelHash = generationParams.ModelHash,
				CFGScale = generationParams.CFGScale,
				ClipSkip = generationParams.ClipSkip,
				DenoisingStrength = generationParams.DenoisingStrength,
				Model = generationParams.Model,
				Sampler = generationParams.Sampler,
				Seed = generationParams.Seed,
				Size = generationParams.Size,
				Steps = generationParams.Steps,
				BatchPos = generationParams.BatchPos,
				BatchSize = generationParams.BatchSize,
				ENSD = generationParams.ENSD,
				Eta = generationParams.Eta,
				FaceRestoration = generationParams.FaceRestoration,
				FirstPassSize = generationParams.FirstPassSize,
				HypernetStrength = generationParams.HypernetStrength,
				HypernetHash = generationParams.HypernetHash,
				Hypernet = generationParams.Hypernet,
				MaskBlur = generationParams.MaskBlur,
				VariationSeed = generationParams.VariationSeed,
				VariationSeedStrength = generationParams.VariationSeedStrength,
				SeedResizeFrom = generationParams.SeedResizeFrom,
				HiresResize = generationParams.HiresResize,
				HiresUpscaler = generationParams.HiresUpscaler,
			};
		}

		private class CSVEntry
		{
			public string Filename { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public string Prompt { get; set; }
			public string NegativePrompt { get; set; }
			public string Parameters { get; set; }
			public string Warnings { get; set; }
			public string Steps { get; set; }
			public string Sampler { get; set; }
			public string CFGScale { get; set; }
			public string Seed { get; set; }
			public string Size { get; set; }
			public string ModelHash { get; set; }
			public string Model { get; set; }
			public string ClipSkip { get; set; }
			public string DenoisingStrength { get; set; }
			public string BatchSize { get; set; }
			public string BatchPos { get; set; }
			public string FaceRestoration { get; set; }
			public string Eta { get; set; }
			public string FirstPassSize { get; set; }
			public string ENSD { get; set; }
			public string Hypernet { get; set; }
			public string HypernetHash { get; set; }
			public string HypernetStrength { get; set; }
			public string MaskBlur { get; set; }
			public string VariationSeed { get; set; }
			public string VariationSeedStrength { get; set; }
			public string SeedResizeFrom { get; internal set; }
			public string HiresResize { get; set; }
			public string HiresUpscaler { get; set; }
			public int Count { get; set; }
		}
	}
}
