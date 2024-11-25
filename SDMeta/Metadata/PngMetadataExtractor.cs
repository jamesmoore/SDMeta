using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SDMeta.Metadata
{
    public class PngMetadataExtractor
    {
        private static readonly byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static IEnumerable<KeyValuePair<string, string>> ExtractTextualInformation(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open);

            if (!VerifyPngSignature(fs))
            {
                throw new InvalidDataException("Not a valid PNG file.");
            }

            while (fs.Position < fs.Length)
            {
                var chunkLength = ReadChunkLength(fs);
                var chunkType = ReadChunkType(fs);

                switch (chunkType)
                {
                    case "IEND":
                        SkipBytes(fs, 4);
                        break;
                    case "tEXt":
                        var keywordValuePair = ReadTextualData(fs, chunkLength);
                        SkipBytes(fs, 4); // Move past the current chunk CRC
                        yield return keywordValuePair;
                        break;
                    default:
                        SkipBytes(fs, chunkLength + 4); // Move past the current chunk and its CRC
                        break;
                }
            }
        }

        private static bool VerifyPngSignature(Stream stream)
        {
            var actualSignature = new byte[pngSignature.Length];
            var bytesRead = stream.Read(actualSignature, 0, actualSignature.Length);
            return bytesRead == pngSignature.Length && !actualSignature.Where((t, i) => t != pngSignature[i]).Any();
        }

        private static int ReadChunkLength(Stream stream)
        {
            var buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) != 4)
            {
                throw new EndOfStreamException("Unexpected end of file while reading chunk length.");
            }
            return BitConverter.ToInt32(buffer.Reverse().ToArray(), 0); // Reverse for big endian
        }

        private static string ReadChunkType(Stream stream)
        {
            var buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) != 4)
            {
                throw new EndOfStreamException("Unexpected end of file while reading chunk type.");
            }
            return Encoding.ASCII.GetString(buffer);
        }

        private static KeyValuePair<string, string> ReadTextualData(Stream stream, int length)
        {
            var buffer = new byte[length];
            if (stream.Read(buffer, 0, length) != length)
            {
                throw new EndOfStreamException("Unexpected end of file while reading textual data.");
            }

            var dataString = BytesToString(buffer);
            var nullIndex = dataString.IndexOf('\0');
            return new KeyValuePair<string, string>(
                nullIndex > -1 ? dataString.Substring(0, nullIndex) : "",
                nullIndex > -1 && nullIndex + 1 < length ? dataString.Substring(nullIndex + 1).TrimEnd('\0') : ""
            );
        }

        private static string BytesToString(byte[] buffer)
        {
            try
            {
                var utf8EncoderWithErrorCatching = new UTF8Encoding(false, true);
                return utf8EncoderWithErrorCatching.GetString(buffer).TrimEnd((char)0).Trim();
            }
            catch (DecoderFallbackException)
            {
                var fallbackEncoder = Encoding.GetEncoding("iso-8859-1");
                return fallbackEncoder.GetString(buffer).TrimEnd((char)0).Trim();
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
