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
			var nodes = JsonSerializer.Deserialize<Dictionary<string, UntypedBaseNode>>(_parameters);

			var typedNodes = nodes.Select(p => p.Value.GetInputs(p.Key)).ToList();

			var modelNode = typedNodes.OfType<CheckpointLoaderSimpleInputs>().OrderBy(p => p.ckpt_name.ToLower().Contains("refiner")).FirstOrDefault();

			var samplerNode = typedNodes.OfType<KSamplerBase>().ToList();

			var clipText = typedNodes.OfType<CLIPTextEncodeInputs>().ToList();

			var posNeg = samplerNode.Select(p => p.GetClips(clipText)).Distinct().ToList();

			var positive = posNeg.Select(p => p.positive?.Trim()).OrderBy(p => p).Aggregate((p, q) => p + " " + q);
			var negative = posNeg.Select(p => p.negative?.Trim()).OrderBy(p => p).Aggregate((p, q) => p + " " + q);

			return new GenerationParams()
			{
				Model = modelNode?.ckpt_name,
				Prompt = positive,
				NegativePrompt = negative,
			};
		}
	}

	public class UntypedBaseNode
	{
		public string class_type { get; set; }
		public JsonNode inputs { get; set; }

		public BaseInputs? GetInputs(string nodeId)
		{
			var node = GetNode();
			if (node != null)
			{
				node.NodeId = nodeId;
			}
			return node;
		}

		private BaseInputs? GetNode() => class_type switch
		{
			"CheckpointLoaderSimple" => inputs.Deserialize<CheckpointLoaderSimpleInputs>(),
			"CLIPTextEncode" => inputs.Deserialize<CLIPTextEncodeInputs>(),
			"KSampler" => inputs.Deserialize<KSamplerInputs>(),
			"KSamplerAdvanced" => inputs.Deserialize<KSamplerAdvancedInputs>(),
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
	}

	public class CLIPTextEncodeInputs : BaseInputs
	{
		public string text { get; set; }
		public JsonArray clip { get; set; }
	}

	public class KSamplerBase : BaseInputs
	{
		public JsonArray model { get; set; }
		public JsonArray positive { get; set; }
		public JsonArray negative { get; set; }
		public JsonArray latent_image { get; set; }

		public (string? positive, string? negative) GetClips(IEnumerable<CLIPTextEncodeInputs> clips)
		{
			var positiveNodeId = positive?.FirstOrDefault()?.ToString();
			var negativeNodeId = negative?.FirstOrDefault()?.ToString();

			return (
				clips.FirstOrDefault(p => p.NodeId == positiveNodeId)?.text,
				clips.FirstOrDefault(p => p.NodeId == negativeNodeId)?.text
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
}
