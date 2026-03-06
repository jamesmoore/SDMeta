using System.IO.Abstractions;
using SDMeta.Metadata;

namespace SDMetaTest.Metadata
{
    [TestClass]
    public class PngMetadataExtractorTests
    {
        [TestMethod]
        public async Task PngMetadataExtractorTest()
        {
            using var fs = new FileSystem().FileStream.New("./Metadata/latin1-pngtext.png", FileMode.Open);

            var items = PngMetadataExtractor.ExtractTextualInformation(fs);
            var metadata = await items.ToDictionaryAsync(p => p.Key, p => p.Value);

            Assert.IsNotNull(metadata);
            Assert.HasCount(1, metadata);
            Assert.IsTrue(metadata.ContainsKey("parameters"));
        
            var prompt = metadata["parameters"];
            Assert.IsNotNull(prompt);
            Assert.AreEqual(309, prompt.Length);
            Assert.StartsWith("Quince", prompt);
            Assert.Contains("Sánchez", prompt);
            Assert.EndsWith("v1.8.0", prompt);
        }

        [TestMethod]
        public async Task PngMetadataExtractorTest_iTXt_Uncompressed()
        {
            using var fs = new FileSystem().FileStream.New("./Metadata/itxt-uncompressed.png", FileMode.Open);

            var items = PngMetadataExtractor.ExtractTextualInformation(fs);
            var metadata = await items.ToDictionaryAsync(p => p.Key, p => p.Value);

            Assert.IsNotNull(metadata);
            Assert.HasCount(1, metadata);
            Assert.IsTrue(metadata.ContainsKey("parameters"));

            var prompt = metadata["parameters"];
            Assert.IsNotNull(prompt);
            Assert.AreEqual("steps: 20, sampler: Euler a, cfg scale: 7, seed: 12345, size: 512x512, model: v1-5-pruned", prompt);
        }

        [TestMethod]
        public async Task PngMetadataExtractorTest_iTXt_Compressed()
        {
            using var fs = new FileSystem().FileStream.New("./Metadata/itxt-compressed.png", FileMode.Open);

            var items = PngMetadataExtractor.ExtractTextualInformation(fs);
            var metadata = await items.ToDictionaryAsync(p => p.Key, p => p.Value);

            Assert.IsNotNull(metadata);
            Assert.HasCount(1, metadata);
            Assert.IsTrue(metadata.ContainsKey("parameters"));

            var prompt = metadata["parameters"];
            Assert.IsNotNull(prompt);
            Assert.AreEqual("steps: 30, sampler: DPM++ 2M Karras, cfg scale: 8, seed: 67890, size: 768x768, model: v2-1", prompt);
        }
    }
}
