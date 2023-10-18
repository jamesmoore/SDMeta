using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SDMeta.Comfy
{
	public class ComfyUIParameterDecoder : IParameterDecoder
	{
		public GenerationParams GetParameters(string _parameters)
		{
			var nodes = JsonSerializer.Deserialize<Dictionary<string, BaseNode>>(_parameters);

			var modelNode = nodes.FirstOrDefault(p => p.Value.class_type == "CheckpointLoaderSimple");

			var promptNodes = nodes.Where(p => p.Value.class_type == "CLIPTextEncode");



			return new GenerationParams()
			{
				Model = modelNode.Value?.inputs["ckpt_name"].ToString(),
			};
		}
	}

	public class BaseNode
	{
		public string class_type { get; set; }
		public Dictionary<string, object> inputs { get; set; }
	}
}
