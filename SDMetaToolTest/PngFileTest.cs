using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDMetaTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDMetaToolTest
{
    [TestClass]
    public class PngFileTest
    {
        [TestMethod]
        public void PngFile_GetParameters_Test()
        {
            var sut = new PngFile()
            {
                Parameters = @"(cute:1.1), chibis in sims 1,
(isometric:1.2) game, at gallery of artworks, touhou, NPC, kitsuke, surreal, fuji yama, otaku clutter, brick a brac, souvenirs, objets d' art,
by Katsushika Hokusai and Kitagawa Utamaro, Ukiyo-e, traditional media, woodblock print, Utagawa Hiroshige, Katsushika Hokusai,

Negative prompt: lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, low quality, normal quality, jpeg artifacts,signature, watermark, username, blurry, artist name
Steps: 30, Sampler: DPM++ 2M Karras, CFG scale: 11, Seed: 358940890, Size: 704x704, Model hash: 2700c435, Model: Anything-V3.0-pruned, Clip skip: 2",
            };

            var parameters = sut.GetParameters();
            Assert.IsNotNull(parameters);
            StringAssert.StartsWith(parameters.Prompt, "(cute" );
            StringAssert.EndsWith(parameters.Prompt, "Hokusai," );

            StringAssert.StartsWith(parameters.NegativePrompt, "lowres");
            StringAssert.EndsWith(parameters.NegativePrompt, "artist name");
        }


    }
}
