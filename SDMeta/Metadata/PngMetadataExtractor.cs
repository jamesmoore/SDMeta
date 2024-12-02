using System;
using System.Collections.Generic;
using System.IO;
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

            var dataString = BytesToString(buffer).TrimEnd(NullTerminator).Trim();
            var nullIndex = dataString.IndexOf(NullTerminator);
            return new (
                nullIndex > -1 ? dataString.Substring(0, nullIndex) : string.Empty,
                nullIndex > -1 && nullIndex + 1 < length ? dataString.Substring(nullIndex + 1).TrimEnd(NullTerminator) : string.Empty
            );
        }

        private static readonly Encoding uTF8Encoding = new UTF8Encoding(false, true);
        private static readonly Encoding fallbackEncoder = Encoding.GetEncoding("iso-8859-1");

        private static string BytesToString(byte[] buffer)
        {
            try
            {
                return uTF8Encoding.GetString(buffer);
            }
            catch (DecoderFallbackException)
            {
                return fallbackEncoder.GetString(buffer);
            }
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
