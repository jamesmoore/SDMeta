using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SDMetaTool.Auto1111
{
	public partial class Auto1111ParameterDecoder : IParameterDecoder
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private const string NegativePromptPrefix = "Negative prompt:";
		private const string SingleParameterRegexString = """\s*([\w\/ ]+):\s*("(?:\\"[^,]|\\"|\\|[^\"])+"|[^,]*)(?:,|$)""";
		private const string MultipleParameterRegexString = "^(?:" + SingleParameterRegexString + "){3,}$";
		private const string ImageSize = @"^(\d+)x(\d+)$";
		private const string WildcardPrompt = @",?\s?Wildcard prompt: ""[\S\s]*""";
		private const string ParamModelHash = "Model hash";
		private const string ParamSteps = "Steps";
		private const string ParamSampler = "Sampler";
		private const string ParamCFGScale = "CFG scale";
		private const string ParamSize = "Size";
		private const string ParamClipSkip = "Clip skip";
		private const string ParamSeed = "Seed";
		private const string ParamModel = "Model";
		private const string ParamDenoisingStrength = "Denoising strength";
		private const string ParamBatchSize = "Batch size";
		private const string ParamBatchPos = "Batch pos";
		private const string ParamFaceRestoration = "Face restoration";
		private const string ParamEta = "Eta";
		private const string ParamFirstPassSize = "First pass size";
		private const string ParamENSD = "ENSD";
		private const string ParamHypernet = "Hypernet";
		private const string ParamHypernetHash = "Hypernet hash";
		private const string ParamHypernetStrength = "Hypernet strength";
		private const string ParamMaskBlur = "Mask blur";
		private const string ParamVariationSeed = "Variation seed";
		private const string ParamVariationSeedStrength = "Variation seed strength";
		private const string ParamSeedResizeFrom = "Seed resize from";
		private const string ParamHiresResize = "Hires resize";
		private const string ParamHiresUpscaler = "Hires upscaler";
		private const string ParamHiresUpscale = "Hires upscale";
		private const string ParamHiresSteps = "Hires steps";

		private static readonly string[] KnownParams = new[]
		{
			ParamModel,
			ParamModelHash,
			ParamSteps,
			ParamSampler,
			ParamCFGScale,
			ParamSize,
			ParamClipSkip,
			ParamSeed,
			ParamDenoisingStrength,
			ParamBatchSize,
			ParamBatchPos,
			ParamFaceRestoration,
			ParamEta,
			ParamFirstPassSize,
			ParamENSD,
			ParamHypernet,
			ParamHypernetHash,
			ParamHypernetStrength,
			ParamMaskBlur,
			ParamVariationSeed,
			ParamVariationSeedStrength,
			ParamSeedResizeFrom,
			ParamHiresResize,
			ParamHiresUpscaler,
			ParamHiresUpscale,
			ParamHiresSteps,
		};

		public GenerationParams GetParameters(string _parameters)
		{
			if (string.IsNullOrWhiteSpace(_parameters))
			{
				return new Auto1111GenerationParams();
			}

			var re_imagesize = ImageSizeRegex();

			var withWildcardRemoved = WildcardPromptRegex().Replace(_parameters, "");

			var fullList = withWildcardRemoved.Trim().Split('\n').Select(p => p.Trim()).ToList();

			var warningLine = fullList.FirstOrDefault(p => p.StartsWith("Warning:"));

			var warningLineString = null as string;

			if (warningLine != null)
			{
				var warningStart = fullList.IndexOf(warningLine);
				var warningLines = fullList.Skip(warningStart).ToList();
				fullList = fullList.Take(warningStart).Where(p => string.IsNullOrWhiteSpace(p) == false).ToList();
				warningLineString = string.Join('\n', warningLines);
			}

			var lines = fullList;
			var lastLine = lines.Last();

			var parameters = string.Empty;

			var paramsMatch = MultipleParameterRegex().Match(lastLine);

			var parametersLookup = Enumerable.Empty<string>().ToLookup(p => p, p => p);

			if (paramsMatch.Success)
			{
				parameters = lastLine;
				lines = lines.Take(lines.Count - 1).ToList();
				parametersLookup = DecodeParamsLine(lastLine);
			}

			(string positive, string negative) = SplitPrompts(lines);

			return new Auto1111GenerationParams()
			{
				Prompt = positive,
				NegativePrompt = negative,
				Params = parameters,
				Warnings = warningLineString,
				ModelHash = parametersLookup[ParamModelHash]?.FirstOrDefault(),
				Steps = parametersLookup[ParamSteps]?.FirstOrDefault(),
				Sampler = parametersLookup[ParamSampler]?.FirstOrDefault(),
				CFGScale = parametersLookup[ParamCFGScale]?.FirstOrDefault(),
				Size = parametersLookup[ParamSize]?.FirstOrDefault(),
				ClipSkip = parametersLookup[ParamClipSkip]?.FirstOrDefault(),
				Seed = parametersLookup[ParamSeed]?.FirstOrDefault(),
				Model = parametersLookup[ParamModel]?.FirstOrDefault(),
				DenoisingStrength = parametersLookup[ParamDenoisingStrength]?.FirstOrDefault(),
				BatchSize = parametersLookup[ParamBatchSize]?.FirstOrDefault(),
				BatchPos = parametersLookup[ParamBatchPos]?.FirstOrDefault(),
				FaceRestoration = parametersLookup[ParamFaceRestoration]?.FirstOrDefault(),
				Eta = parametersLookup[ParamEta]?.FirstOrDefault(),
				FirstPassSize = parametersLookup[ParamFirstPassSize]?.FirstOrDefault(),
				ENSD = parametersLookup[ParamENSD]?.FirstOrDefault(),
				MaskBlur = parametersLookup[ParamMaskBlur]?.FirstOrDefault(),
				Hypernet = parametersLookup[ParamHypernet]?.FirstOrDefault(),
				HypernetHash = parametersLookup[ParamHypernetHash]?.FirstOrDefault(),
				HypernetStrength = parametersLookup[ParamHypernetStrength]?.FirstOrDefault(),
				VariationSeed = parametersLookup[ParamVariationSeed]?.FirstOrDefault(),
				VariationSeedStrength = parametersLookup[ParamVariationSeedStrength]?.FirstOrDefault(),
				SeedResizeFrom = parametersLookup[ParamSeedResizeFrom]?.FirstOrDefault(),
				HiresResize = parametersLookup[ParamHiresResize]?.FirstOrDefault(),
				HiresUpscaler = parametersLookup[ParamHiresUpscaler]?.FirstOrDefault(),
				HiresUpscale = parametersLookup[ParamHiresUpscale]?.FirstOrDefault(),
				HiresSteps = parametersLookup[ParamHiresSteps]?.FirstOrDefault(),
				PromptHash = ComputeSha256Hash(WhitespaceRegex().Replace(positive, " ").ToLower()),
				NegativePromptHash = ComputeSha256Hash(WhitespaceRegex().Replace(negative, " ").ToLower()),
			};
		}

		private static ILookup<string, string> DecodeParamsLine(string lastLine)
		{
			var parametersDecoded = SingleParameterRegex().Matches(lastLine);

			var parametersLookup = parametersDecoded.Select(p => new { Key = p.Groups[1].Value, Value = p.Groups[2].Value }).ToLookup(p => p.Key, p => p.Value);

			var extraKeys = parametersLookup.Select(p => p.Key).Except(KnownParams).ToList();
			if (extraKeys.Any())
			{
				logger.Warn("Unknown param: " + string.Join(",", extraKeys));
			}

			return parametersLookup;
		}

		private static (string positive, string negative) SplitPrompts(List<string> lines)
		{
			var positive = new List<string>();
			var negative = new List<string>();
			var negativeStart = lines.FirstOrDefault(p => p.StartsWith(NegativePromptPrefix));
			if (negativeStart != null)
			{
				var negativePosition = lines.IndexOf(negativeStart);
				positive = lines.Take(negativePosition).ToList();
				negative = lines.Skip(negativePosition).ToList();
				negative[0] = negative[0][NegativePromptPrefix.Length..];
			}
			else
			{
				positive = lines;
			}

			var prompt = string.Join('\n', positive).Trim();
			var negativeString = string.Join('\n', negative).Trim();

			return (prompt, negativeString);
		}


		[GeneratedRegex(SingleParameterRegexString)]
		private static partial Regex SingleParameterRegex();
		[GeneratedRegex(MultipleParameterRegexString)]
		public static partial Regex MultipleParameterRegex();
		[GeneratedRegex(ImageSize)]
		private static partial Regex ImageSizeRegex();
		[GeneratedRegex(WildcardPrompt)]
		private static partial Regex WildcardPromptRegex();
		[GeneratedRegex(@"\s+")]
		private static partial Regex WhitespaceRegex();

		static string ComputeSha256Hash(string rawData)
		{
			// Create a SHA256   
			// ComputeHash - returns byte array  
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

			// Convert byte array to a string   
			StringBuilder builder = new();
			for (int i = 0; i < bytes.Length; i++)
			{
				builder.Append(bytes[i].ToString("x2"));
			}
			return builder.ToString();
		}
	}
}
