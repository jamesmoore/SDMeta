using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDMeta.Metadata
{
    public class PngMetadataExtractor
    {
        private const char NullTerminator = (char)0;
        private static readonly byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public async static IAsyncEnumerable<(string Key, string Value)> ExtractTextualInformation(Stream fs)
        {
            if (!await VerifyPngSignature(fs))
            {
                throw new InvalidDataException("Not a valid PNG file.");
            }

            while (fs.Position < fs.Length)
            {
                var chunkLength = await ReadChunkLength(fs);
                var chunkType = await ReadChunkType(fs);

                switch (chunkType)
                {
                    case "IEND":
                        SkipBytes(fs, 4);
                        break;
                    case "tEXt":
                        var keywordValuePair = await ReadTextualData(fs, chunkLength);
                        SkipBytes(fs, 4); // Move past the current chunk CRC
                        yield return keywordValuePair;
                        break;
                    case "iTXt":
                        var itxtKeywordValuePair = await ReadInternationalTextualData(fs, chunkLength);
                        SkipBytes(fs, 4); // Move past the current chunk CRC
                        yield return itxtKeywordValuePair;
                        break;
                    default:
                        SkipBytes(fs, chunkLength + 4); // Move past the current chunk and its CRC
                        break;
                }
            }
        }

        private async static Task<bool> VerifyPngSignature(Stream stream)
        {
            var actualSignature = new byte[pngSignature.Length];
            var bytesRead = await stream.ReadAsync(actualSignature);
            return actualSignature.SequenceEqual(pngSignature);
        }

        private async static Task<int> ReadChunkLength(Stream stream)
        {
            var buffer = new byte[4];
            if (await stream.ReadAsync(buffer) != 4)
            {
                throw new EndOfStreamException("Unexpected end of file while reading chunk length.");
            }
            return BitConverter.ToInt32(buffer.Reverse().ToArray()); // Reverse for big endian
        }

        private async static Task<string> ReadChunkType(Stream stream)
        {
            var buffer = new byte[4];
            if (await stream.ReadAsync(buffer) != 4)
            {
                throw new EndOfStreamException("Unexpected end of file while reading chunk type.");
            }
            return Encoding.ASCII.GetString(buffer);
        }

        private async static Task<(string Key, string Value)> ReadTextualData(Stream stream, int length)
        {
            var buffer = new byte[length];
            if (await stream.ReadAsync(buffer) != length)
            {
                throw new EndOfStreamException("Unexpected end of file while reading textual data.");
            }

            var dataString = buffer.BytesToString().TrimEnd(NullTerminator).Trim();
            var nullIndex = dataString.IndexOf(NullTerminator);
            return new (
                nullIndex > -1 ? dataString.Substring(0, nullIndex) : string.Empty,
                nullIndex > -1 && nullIndex + 1 < length ? dataString.Substring(nullIndex + 1).Trim(NullTerminator) : string.Empty
            );
        }

        private async static Task<(string Key, string Value)> ReadInternationalTextualData(Stream stream, int length)
        {
            var buffer = new byte[length];
            if (await stream.ReadAsync(buffer) != length)
            {
                throw new EndOfStreamException("Unexpected end of file while reading iTXt data.");
            }

            // iTXt layout (all indices into buffer):
            //   [keyword]\0 [compression_flag:1] [compression_method:1]
            //   [language_tag]\0 [translated_keyword]\0 [text...]
            var keywordEnd = Array.IndexOf(buffer, (byte)0);
            if (keywordEnd < 0 || keywordEnd + 4 > buffer.Length)
            {
                return (string.Empty, string.Empty);
            }

            var keyword = Encoding.UTF8.GetString(buffer, 0, keywordEnd);
            var compressionFlag = buffer[keywordEnd + 1];
            var compressionMethod = buffer[keywordEnd + 2];
            // compression_method is at keywordEnd + 2; only method 0 (zlib) is defined
            var afterFlags = keywordEnd + 3;

            // Skip language tag (null-terminated)
            var languageEnd = Array.IndexOf(buffer, (byte)0, afterFlags);
            if (languageEnd < 0 || languageEnd + 1 > buffer.Length)
            {
                return (keyword, string.Empty);
            }

            // Skip translated keyword (null-terminated)
            var translatedKeywordEnd = Array.IndexOf(buffer, (byte)0, languageEnd + 1);
            if (translatedKeywordEnd < 0 || translatedKeywordEnd + 1 > buffer.Length)
            {
                return (keyword, string.Empty);
            }

            var textStart = translatedKeywordEnd + 1;
            var textBytes = buffer.AsSpan(textStart).ToArray();

            string text;
            if (compressionFlag == 1)
            {
                if (compressionMethod != 0)
                {
                    throw new InvalidDataException($"Unsupported iTXt compression method: {compressionMethod}. Only zlib deflate (method 0) is supported.");
                }
                using var compressed = new MemoryStream(textBytes);
                using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
                using var decompressed = new MemoryStream();
                await zlib.CopyToAsync(decompressed);
                text = Encoding.UTF8.GetString(decompressed.ToArray());
            }
            else
            {
                text = Encoding.UTF8.GetString(textBytes);
            }

            return (keyword, text);
        }

        private static void SkipBytes(Stream stream, int bytesToSkip)
        {
            var originalPosition = stream.Position;
            var newPosition = stream.Seek(bytesToSkip, SeekOrigin.Current);
            if (newPosition != originalPosition + bytesToSkip)
            {
                throw new IOException("Failed to skip chunk and CRC.");
            }
        }
    }
}
