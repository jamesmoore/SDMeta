using SDMeta.Comfy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDMetaTest.ComfyUI
{
	[TestClass]
	public class ComfyUIParameterDecoderTest
	{
		[TestMethod]
		public void GetParams()
		{
			var comfyParams = new ComfyUIParameterDecoder().GetParameters(testJson);
			Assert.AreEqual("breakdomain_M2000.safetensors", comfyParams.Model);
		}

		const string testJson = """
		{
		    "638": {
		        "inputs": {
		            "ckpt_name": "breakdomain_M2000.safetensors"
		        },
		        "class_type": "CheckpointLoaderSimple"
		    },
		    "639": {
		        "inputs": {
		            "seed": 327183236491815,
		            "steps": 30,
		            "cfg": 8.0,
		            "sampler_name": "dpmpp_2m",
		            "scheduler": "normal",
		            "denoise": 1.0,
		            "model": [
		                "691",
		                0
		            ],
		            "positive": [
		                "641",
		                0
		            ],
		            "negative": [
		                "642",
		                0
		            ],
		            "latent_image": [
		                "655",
		                0
		            ]
		        },
		        "class_type": "KSampler"
		    },
		    "640": {
		        "inputs": {
		            "vae_name": "sd v1-5-pruned-emaonly.vae.pt"
		        },
		        "class_type": "VAELoader"
		    },
		    "641": {
		        "inputs": {
		            "text": "close up, verdant, flowers, tropical, absurdres, best quality",
		            "clip": [
		                "691",
		                1
		            ]
		        },
		        "class_type": "CLIPTextEncode"
		    },
		    "642": {
		        "inputs": {
		            "text": "(worst quality, low quality:1.2), (text, signature, logo, watermark)",
		            "clip": [
		                "691",
		                1
		            ]
		        },
		        "class_type": "CLIPTextEncode"
		    },
		    "648": {
		        "inputs": {
		            "seed": 620375337261021,
		            "steps": 30,
		            "cfg": 8.0,
		            "sampler_name": "dpmpp_2m",
		            "scheduler": "normal",
		            "denoise": 0.5,
		            "model": [
		                "691",
		                0
		            ],
		            "positive": [
		                "641",
		                0
		            ],
		            "negative": [
		                "642",
		                0
		            ],
		            "latent_image": [
		                "664",
		                0
		            ]
		        },
		        "class_type": "KSampler"
		    },
		    "655": {
		        "inputs": {
		            "width": 896,
		            "height": 384,
		            "batch_size": 1
		        },
		        "class_type": "EmptyLatentImage"
		    },
		    "656": {
		        "inputs": {
		            "samples": [
		                "648",
		                0
		            ],
		            "vae": [
		                "640",
		                0
		            ]
		        },
		        "class_type": "VAEDecode"
		    },
		    "657": {
		        "inputs": {
		            "model_name": "4x-UltraSharp.pth"
		        },
		        "class_type": "UpscaleModelLoader"
		    },
		    "658": {
		        "inputs": {
		            "upscale_model": [
		                "657",
		                0
		            ],
		            "image": [
		                "656",
		                0
		            ]
		        },
		        "class_type": "ImageUpscaleWithModel"
		    },
		    "660": {
		        "inputs": {
		            "seed": 1022161813650116,
		            "steps": 30,
		            "cfg": 8.0,
		            "sampler_name": "dpmpp_2m",
		            "scheduler": "normal",
		            "denoise": 0.4,
		            "model": [
		                "691",
		                0
		            ],
		            "positive": [
		                "641",
		                0
		            ],
		            "negative": [
		                "642",
		                0
		            ],
		            "latent_image": [
		                "694",
		                0
		            ]
		        },
		        "class_type": "KSampler"
		    },
		    "661": {
		        "inputs": {
		            "upscale_method": "nearest-exact",
		            "width": 3584,
		            "height": 1536,
		            "crop": "disabled",
		            "image": [
		                "658",
		                0
		            ]
		        },
		        "class_type": "ImageScale"
		    },
		    "664": {
		        "inputs": {
		            "upscale_method": "nearest-exact",
		            "width": 1792,
		            "height": 768,
		            "crop": "disabled",
		            "samples": [
		                "639",
		                0
		            ]
		        },
		        "class_type": "LatentUpscale"
		    },
		    "665": {
		        "inputs": {
		            "stop_at_clip_layer": -1,
		            "clip": [
		                "638",
		                1
		            ]
		        },
		        "class_type": "CLIPSetLastLayer"
		    },
		    "686": {
		        "inputs": {
		            "filename_prefix": "ComfyUI",
		            "images": [
		                "695",
		                0
		            ]
		        },
		        "class_type": "SaveImage"
		    },
		    "687": {
		        "inputs": {
		            "filename_prefix": "ComfyUI",
		            "images": [
		                "688",
		                0
		            ]
		        },
		        "class_type": "SaveImage"
		    },
		    "688": {
		        "inputs": {
		            "samples": [
		                "648",
		                0
		            ],
		            "vae": [
		                "640",
		                0
		            ]
		        },
		        "class_type": "VAEDecode"
		    },
		    "691": {
		        "inputs": {
		            "lora_name": "add_detail.safetensors",
		            "strength_model": -1.5,
		            "strength_clip": -1.5,
		            "model": [
		                "693",
		                0
		            ],
		            "clip": [
		                "693",
		                1
		            ]
		        },
		        "class_type": "LoraLoader"
		    },
		    "693": {
		        "inputs": {
		            "lora_name": "akihikoYoshidaBravelyDefault_1.safetensors",
		            "strength_model": 0.3,
		            "strength_clip": 0.3,
		            "model": [
		                "638",
		                0
		            ],
		            "clip": [
		                "665",
		                0
		            ]
		        },
		        "class_type": "LoraLoader"
		    },
		    "694": {
		        "inputs": {
		            "pixels": [
		                "661",
		                0
		            ],
		            "vae": [
		                "640",
		                0
		            ]
		        },
		        "class_type": "VAEEncodeTiled"
		    },
		    "695": {
		        "inputs": {
		            "samples": [
		                "660",
		                0
		            ],
		            "vae": [
		                "640",
		                0
		            ]
		        },
		        "class_type": "VAEDecodeTiled"
		    }
		}
		""";
	}


}
