using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SDMeta.Comfy
{
	public class ComfyUIParameterDecoder : IParameterDecoder
	{
		public GenerationParams GetParameters(string _parameters)
		{
			try
			{
				var nodes = JsonSerializer.Deserialize<Dictionary<string, UntypedBaseNode>>(_parameters);

				var typedNodes = nodes.Select(p => p.Value.GetInputs(p.Key)).ToList();

				var modelNode = typedNodes.OfType<CheckpointLoaderSimpleInputs>().OrderBy(p => p.IsRefiner()).FirstOrDefault();

				var samplerNode = typedNodes.OfType<KSamplerBase>().ToList();

				var clipText = typedNodes.OfType<BaseCLIPTestEncodeInputs>().ToList();

				var posNeg = samplerNode.Select(p => p.GetClips(clipText)).Distinct().ToList();

				var positive = posNeg.Select(p => p.positive?.Trim()).DefaultIfEmpty("").OrderBy(p => p).Aggregate((p, q) => p + " " + q);
				var negative = posNeg.Select(p => p.negative?.Trim()).DefaultIfEmpty("").OrderBy(p => p).Aggregate((p, q) => p + " " + q);

				return new GenerationParams()
				{
					Model = modelNode?.GetCheckpointName(),
					Prompt = positive,
					NegativePrompt = negative,
				};
			}
			catch(Exception ex)
			{
				const string errorMessage = "Unable to decode Comfy prompt";
				return new GenerationParams()
				{
					Model = errorMessage,
					Prompt = errorMessage,
					NegativePrompt = errorMessage,
				};
			}
		}
	}

	public class UntypedBaseNode
	{
		public string class_type { get; set; }
		public JsonNode inputs { get; set; }

		public BaseInputs? GetInputs(string nodeId)
		{
			try
			{
				var node = GetNode();
				if (node != null)
				{
					node.NodeId = nodeId;
				}
				return node;
			}
			catch (Exception ex)
			{
				return new BaseInputs()
				{
					NodeId = nodeId,
				};
			}
		}

		private BaseInputs? GetNode() => class_type switch
		{
			"CheckpointLoaderSimple" => inputs.Deserialize<CheckpointLoaderSimpleInputs>(),
			"CLIPTextEncode" => inputs.Deserialize<CLIPTextEncodeInputs>(),
			"KSampler" => inputs.Deserialize<KSamplerInputs>(),
			"KSamplerAdvanced" => inputs.Deserialize<KSamplerAdvancedInputs>(),
			"CLIPTextEncodeSDXL" => inputs.Deserialize<CLIPTextEncodeSDXL>(),
			"CLIPTextEncodeSDXLRefiner" => inputs.Deserialize<CLIPTextEncodeSDXLRefiner>(),
			_ => null
		};
	}

	public class BaseInputs
	{
		public string NodeId { get; set; }
	}

	public class CheckpointLoaderSimpleInputs : BaseInputs
	{
		public string ckpt_name { get; set; }

		public string? GetCheckpointName() => ckpt_name?.Replace(".safetensors", "");

		public bool IsRefiner() => ckpt_name.ToLower().Contains("refiner");
	}

	public abstract class BaseCLIPTestEncodeInputs : BaseInputs
	{
		public JsonArray clip { get; set; }
		public abstract string GetText();
	}

	public class CLIPTextEncodeInputs : BaseCLIPTestEncodeInputs
	{
		public string text { get; set; }

		public override string GetText() => text;
	}

	public class KSamplerBase : BaseInputs
	{
		public JsonArray model { get; set; }
		public JsonArray positive { get; set; }
		public JsonArray negative { get; set; }
		public JsonArray latent_image { get; set; }

		public (string? positive, string? negative) GetClips(IEnumerable<BaseCLIPTestEncodeInputs> clips)
		{
			var positiveNodeId = positive?.FirstOrDefault()?.ToString();
			var negativeNodeId = negative?.FirstOrDefault()?.ToString();

			return (
				clips.FirstOrDefault(p => p.NodeId == positiveNodeId)?.GetText(),
				clips.FirstOrDefault(p => p.NodeId == negativeNodeId)?.GetText()
				);
		}
	}

	public class KSamplerInputs : KSamplerBase
	{
		public long seed { get; set; }
		public int steps { get; set; }
		public float cfg { get; set; }
		public string sampler_name { get; set; }
		public string scheduler { get; set; }
		public float denoise { get; set; }
	}

	public class KSamplerAdvancedInputs : KSamplerBase
	{
		public string add_noise { get; set; }
		public long noise_seed { get; set; }
		public int steps { get; set; }
		public float cfg { get; set; }
		public string sampler_name { get; set; }
		public string scheduler { get; set; }
		public int start_at_step { get; set; }
		public int end_at_step { get; set; }
		public string return_with_leftover_noise { get; set; }
	}

	public class CLIPTextEncodeSDXL : BaseCLIPTestEncodeInputs
	{
		public int width { get; set; }
		public int height { get; set; }
		public int crop_w { get; set; }
		public int crop_h { get; set; }
		public int target_width { get; set; }
		public int target_height { get; set; }
		public string text_g { get; set; }
		public string text_l { get; set; }
		public override string GetText() => (text_g + " " + text_l).Trim();
	}

	public class CLIPTextEncodeSDXLRefiner : BaseCLIPTestEncodeInputs
	{
		public float ascore { get; set; }
		public int width { get; set; }
		public int height { get; set; }
		public string text { get; set; }
		public override string GetText() => text;
	}
}
