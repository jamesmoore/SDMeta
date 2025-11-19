using SDMeta.Metadata;
using System.IO.Abstractions;

namespace SDMetaTest.Metadata
{
    [TestClass]
    public class JpegMetadataExtractorTests
    {
        const string filename = @"./Metadata/00005-2343231048.jpg";

        [TestMethod]
        public async Task JpegMetadataExtractorTest()
        {
            using var fs = new FileSystem().FileStream.New(filename, FileMode.Open);

            var items = JpegMetadataExtractor.ExtractTextualInformation(fs);
            var metadata = await items.ToDictionaryAsync(p => p.Key, p => p.Value);

            Assert.IsNotNull(metadata);
            Assert.HasCount(1, metadata);
            Assert.IsTrue(metadata.ContainsKey("UserComment"));

            var prompt = metadata["UserComment"];
            Assert.IsNotNull(prompt);
            Assert.AreEqual(297, prompt.Length);
            Assert.Contains("pâté", prompt);
        }
    }
}
