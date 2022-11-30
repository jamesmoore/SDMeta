using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDMetaTool;

namespace SDMetaToolTest
{
    [TestClass]
    public class PngFileTest
    {
        [TestMethod]
        public void PngFile_GetParameters_Null_Test()
        {
            var sut = new PngFile()
            {
                Parameters = null
            };

            var parameters = sut.GetParameters();
            Assert.IsNotNull(parameters);
            Assert.AreEqual(string.Empty, parameters.Prompt);
            Assert.AreEqual(string.Empty, parameters.NegativePrompt);
        }

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
            StringAssert.StartsWith(parameters.Prompt, "(cute");
            StringAssert.EndsWith(parameters.Prompt, "Hokusai,");

            StringAssert.StartsWith(parameters.NegativePrompt, "lowres");
            StringAssert.EndsWith(parameters.NegativePrompt, "artist name");
        }

        [TestMethod]
        public void PngFile_GetParameters_With_Warning_Test()
        {
            var sut = new PngFile()
            {
                Parameters = @"Art Nouveau (((Samorost))) ribcage feathers mirror mini isographic concept art extreme tilt-shift 60s soviet animation czechoslovakian hood burnt umbra ectoplasma puppetry on wood trail puppetry Bunraku pop-up-book isographic concept art svankmajer diorama storybook cut out storybook adventure lush forest dark woods leather foliage autumn snow sea_anemone_art_by_hiroshi_yoshida ps1 dreamcast n64 low poly maya blender zbrush
Steps: 24, Sampler: Euler a, CFG scale: 8, Seed: 891571864, Face restoration: CodeFormer, Size: 512x704, Model hash: 7460a6fa

Warning: too many input tokens; some (30) have been truncated:
woods leather foliage autumn snow sea _ anemone _ art _ by _ hiroshi _ yoshida ps 1 dreamcast n 6 4 low poly maya blender zbrush",
            };

            var parameters = sut.GetParameters();
            Assert.IsNotNull(parameters);
            StringAssert.StartsWith(parameters.Prompt, "Art");
            StringAssert.EndsWith(parameters.Prompt, "zbrush");
            
            Assert.AreEqual(string.Empty, parameters.NegativePrompt);

            Assert.AreEqual("Steps: 24, Sampler: Euler a, CFG scale: 8, Seed: 891571864, Face restoration: CodeFormer, Size: 512x704, Model hash: 7460a6fa", parameters.Params);

            StringAssert.StartsWith(parameters.Warnings, "Warning:");
            StringAssert.EndsWith(parameters.Warnings, "zbrush");
        }

        [TestMethod]
        public void PngFile_GetParameters_Positive_Only_Test()
        {
            var sut = new PngFile()
            {
                Parameters = @"cute cat",
            };

            var parameters = sut.GetParameters();
            Assert.IsNotNull(parameters);
            Assert.AreEqual("cute cat", parameters.Prompt);
            Assert.AreEqual(string.Empty, parameters.NegativePrompt);
        }

        [TestMethod]
        public void PngFile_GetParameters_Negative_Only_Test()
        {
            var sut = new PngFile()
            {
                Parameters = "Negative prompt: lowres",
            };

            var parameters = sut.GetParameters();
            Assert.IsNotNull(parameters);
            Assert.AreEqual(string.Empty, parameters.Prompt);
            Assert.AreEqual("lowres", parameters.NegativePrompt);
        }


    }
}
