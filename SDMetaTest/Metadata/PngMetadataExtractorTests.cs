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
            Assert.IsTrue(metadata.Count ==1);
            Assert.IsTrue(metadata.ContainsKey("parameters"));
        
            var prompt = metadata["parameters"];
            Assert.IsNotNull(prompt);
            Assert.AreEqual(309, prompt.Length);
            Assert.IsTrue(prompt.StartsWith("Quince"));
            Assert.IsTrue(prompt.Contains("Sánchez"));
            Assert.IsTrue(prompt.EndsWith("v1.8.0"));
        }
    }
}
