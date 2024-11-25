using SDMeta.Metadata;

namespace SDMetaTest.Metadata
{
    [TestClass]
    public class PngMetadataExtractorTests
    {
        [TestMethod]
        public void PngMetadataExtractorTest()
        {
            var items = PngMetadataExtractor.ExtractTextualInformation("./Metadata/latin1-pngtext.png");
            var metadata = items.ToDictionary(p => p.Key, p => p.Value);

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
