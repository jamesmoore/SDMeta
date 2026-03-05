using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SDMeta.Comfy
{
    public class ComfyUIParameterDecoder(ILogger<ComfyUIParameterDecoder> logger) : IParameterDecoder
    {
        private static readonly JsonSerializerOptions options = new()
        {
            AllowOutOfOrderMetadataProperties = true,
        };

        private static readonly Regex NonStandardNumberRegex = new(
            @"([:\[,]\s*)(NaN|Infinity|-Infinity)(?=\s*[,\}\]])",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static string SanitizeNonStandardJson(string json)
        {
            // Replace non-standard numeric literals that System.Text.Json cannot parse.
            // Preserve the preceding separator and whitespace while replacing the value with null.
            return NonStandardNumberRegex.Replace(json, "$1null");
        }

        public GenerationParams GetParameters(ImageFile imageFile)
        {
            if (imageFile.Prompt == null)
            {
                return GenerationParams.Empty;
            }

            try
            {
                var sanitizedPrompt = SanitizeNonStandardJson(imageFile.Prompt);

                var nodes = JsonSerializer.Deserialize<Dictionary<string, UntypedBaseNode>>(
                    sanitizedPrompt,
                    options);

                if (nodes == null)
                {
                    logger.LogWarning("No nodes found in prompt for file {filename}", imageFile.FileName);
                    return GenerationParams.Empty;
                }

                var typedNodes = nodes.Select(p => p.Value.GetInputs(p.Key)).ToList();

                var modelNode = typedNodes
                    .OfType<ICheckpointLoaderSimpleInputs>()
                    .OrderBy(p => p.IsRefiner())
                    .FirstOrDefault();

                var samplerNode = typedNodes.OfType<KSamplerBase>().ToList();

                var clipText = typedNodes.OfType<BaseCLIPTestEncodeInputs>().ToList();

                var posNeg = samplerNode.Select(p => p.GetClips(clipText)).Distinct().ToList();

                var positive = posNeg
                    .Select(p => p.positive?.Trim())
                    .DefaultIfEmpty("")
                    .OrderBy(p => p)
                    .Aggregate((p, q) => p + " " + q);

                var negative = posNeg
                    .Select(p => p.negative?.Trim())
                    .DefaultIfEmpty("")
                    .OrderBy(p => p)
                    .Aggregate((p, q) => p + " " + q);

                return new GenerationParams
                {
                    Model = modelNode?.GetCheckpointName(),
                    Prompt = positive,
                    NegativePrompt = negative,
                };
            }
            catch (Exception ex)
            {
                const string errorMessage = "Unable to decode Comfy prompt";
                logger.LogError(ex, errorMessage + " for file {filename}", imageFile.FileName);
                return new GenerationParams
                {
                    Model = errorMessage,
                    Prompt = errorMessage,
                    NegativePrompt = errorMessage,
                };
            }
        }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "class_type",
        IgnoreUnrecognizedTypeDiscriminators = true
        )]
    [JsonDerivedType(typeof(CheckpointLoaderSimpleNode), "CheckpointLoaderSimple")]
    [JsonDerivedType(typeof(CLIPTextEncodeNode), "CLIPTextEncode")]
    [JsonDerivedType(typeof(KSamplerNode), "KSampler")]
    [JsonDerivedType(typeof(KSamplerAdvancedNode), "KSamplerAdvanced")]
    [JsonDerivedType(typeof(CLIPTextEncodeSDXLNode), "CLIPTextEncodeSDXL")]
    [JsonDerivedType(typeof(CLIPTextEncodeSDXLRefinerNode), "CLIPTextEncodeSDXLRefiner")]
    [JsonDerivedType(typeof(UNETLoaderNode), "UNETLoader")]
    public class UntypedBaseNode
    {
        public virtual BaseInputs? GetInputs(string nodeId)
        {
            return null;
        }
    }

    public class UntypedBaseNode<T> : UntypedBaseNode where T : BaseInputs
    {
        public T? inputs { get; set; }

        public override BaseInputs? GetInputs(string nodeId)
        {
            if (inputs == null)
            {
                return null;
            }

            inputs.NodeId = nodeId;
            return inputs;
        }
    }

    public sealed class CheckpointLoaderSimpleNode : UntypedBaseNode<CheckpointLoaderSimpleInputs>
    {
    }

    public sealed class CLIPTextEncodeNode : UntypedBaseNode<CLIPTextEncodeInputs>
    {
    }

    public sealed class KSamplerNode : UntypedBaseNode<KSamplerInputs>
    {
    }

    public sealed class KSamplerAdvancedNode : UntypedBaseNode<KSamplerAdvancedInputs>
    {
    }

    public sealed class CLIPTextEncodeSDXLNode : UntypedBaseNode<CLIPTextEncodeSDXL>
    {
    }

    public sealed class CLIPTextEncodeSDXLRefinerNode : UntypedBaseNode<CLIPTextEncodeSDXLRefiner>
    {
    }

    public sealed class UNETLoaderNode : UntypedBaseNode<UNETLoaderInputs>
    {
    }

    public class BaseInputs
    {
        public string? NodeId { get; set; }
    }

    public interface ICheckpointLoaderSimpleInputs
    {
        string? GetCheckpointName();
        bool IsRefiner();
    }

    public class CheckpointLoaderSimpleInputs : BaseInputs, ICheckpointLoaderSimpleInputs
    {
        public string? ckpt_name { get; set; }

        public string? GetCheckpointName() => ckpt_name?.Replace(".safetensors", "");

        public bool IsRefiner() =>
            ckpt_name != null &&
            ckpt_name.Contains("refiner", StringComparison.OrdinalIgnoreCase);
    }

    public abstract class BaseCLIPTestEncodeInputs : BaseInputs
    {
        public JsonArray? clip { get; set; }
        public abstract string? GetText();
    }

    public class CLIPTextEncodeInputs : BaseCLIPTestEncodeInputs
    {
        public string? text { get; set; }

        public override string? GetText() => text;
    }

    public class KSamplerBase : BaseInputs
    {
        public JsonArray? model { get; set; }
        public JsonArray? positive { get; set; }
        public JsonArray? negative { get; set; }
        public JsonArray? latent_image { get; set; }

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
        public float? cfg { get; set; }
        public string? sampler_name { get; set; }
        public string? scheduler { get; set; }
        public float? denoise { get; set; }
    }

    public class KSamplerAdvancedInputs : KSamplerBase
    {
        public string? add_noise { get; set; }
        public long noise_seed { get; set; }
        public int steps { get; set; }
        public float? cfg { get; set; }
        public string? sampler_name { get; set; }
        public string? scheduler { get; set; }
        public int start_at_step { get; set; }
        public int end_at_step { get; set; }
        public string? return_with_leftover_noise { get; set; }
    }

    public class CLIPTextEncodeSDXL : BaseCLIPTestEncodeInputs
    {
        public int width { get; set; }
        public int height { get; set; }
        public int crop_w { get; set; }
        public int crop_h { get; set; }
        public int target_width { get; set; }
        public int target_height { get; set; }
        public string? text_g { get; set; }
        public string? text_l { get; set; }
        public override string? GetText() => (text_g + " " + text_l).Trim();
    }

    public class CLIPTextEncodeSDXLRefiner : BaseCLIPTestEncodeInputs
    {
        public float ascore { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string? text { get; set; }
        public override string? GetText() => text;
    }


    public class UNETLoaderInputs : BaseInputs, ICheckpointLoaderSimpleInputs
    {
        public string? unet_name { get; set; }
        public string? weight_dtype { get; set; }
        public string? GetCheckpointName() => unet_name?.Replace(".safetensors", "");
        public bool IsRefiner() => false;
    }
}
