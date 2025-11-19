using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SDMeta.Metadata
{
    public class JpegMetadataExtractor
    {
        /// <summary>
        /// Extracts textual EXIF information as (Key, Value) pairs from a JPEG stream.
        /// Intended to work with Auto1111 Stable Diffusion JPEGs, including data
        /// stored in the Exif SubIFD pointed to by tag 0x8769.
        /// </summary>
        public async static IAsyncEnumerable<(string Key, string Value)> ExtractTextualInformation(Stream fs)
        {
            if (!await VerifyJpegSignature(fs))
                throw new InvalidDataException("Not a valid JPEG file.");

            // Iterate markers until we find an APP1 Exif segment
            while (true)
            {
                int marker = await ReadByte(fs);
                if (marker == -1) yield break;
                if (marker != 0xFF) continue;

                int markerType = await ReadByte(fs);
                if (markerType == -1) yield break;

                // End of Image or Start of Scan => nothing more useful
                if (markerType == 0xD9 || markerType == 0xDA)
                    yield break;

                // Not APP1: skip that segment
                if (markerType != 0xE1)
                {
                    ushort segLen = await ReadUInt16BE(fs); // includes length bytes
                    if (segLen < 2) yield break;
                    fs.Seek(segLen - 2, SeekOrigin.Current);
                    continue;
                }

                // APP1 segment (potentially EXIF)
                ushort app1Len = await ReadUInt16BE(fs);
                if (app1Len < 2) yield break;

                byte[] exifHeader = new byte[6];
                if (await fs.ReadAsync(exifHeader) != 6) yield break;
                app1Len -= 8; // consumed 2 (length) + 6 (header)

                if (Encoding.ASCII.GetString(exifHeader) != "Exif\0\0")
                {
                    // Not EXIF, skip
                    fs.Seek(app1Len, SeekOrigin.Current);
                    continue;
                }

                // Parse TIFF/EXIF block; offsets in IFDs are relative to this TIFF start
                foreach (var kv in await ParseTiffAsync(fs))
                    yield return kv;

                yield break;
            }
        }

        // ----------------------------------------------------------
        // JPEG helpers
        // ----------------------------------------------------------

        private async static Task<bool> VerifyJpegSignature(Stream fs)
        {
            byte[] sig = new byte[2];
            if (await fs.ReadAsync(sig) != 2) return false;
            return sig[0] == 0xFF && sig[1] == 0xD8; // SOI
        }

        private async static Task<int> ReadByte(Stream s)
        {
            byte[] b = new byte[1];
            int n = await s.ReadAsync(b);
            return n == 0 ? -1 : b[0];
        }

        private async static Task<ushort> ReadUInt16BE(Stream s)
        {
            byte[] b = new byte[2];
            if (await s.ReadAsync(b) != 2) throw new EndOfStreamException();
            return BinaryPrimitives.ReadUInt16BigEndian(b);
        }

        // ----------------------------------------------------------
        // TIFF / EXIF core
        // ----------------------------------------------------------

        private async static Task<List<(string Key, string Value)>> ParseTiffAsync(Stream fs)
        {
            long tiffStart = fs.Position;

            // Byte order: "II" (little) or "MM" (big)
            byte[] order = new byte[2];
            if (await fs.ReadAsync(order) != 2) throw new EndOfStreamException();
            bool littleEndian = order[0] == (byte)'I';

            // Fixed 0x002A
            byte[] mark = new byte[2];
            if (await fs.ReadAsync(mark) != 2) throw new EndOfStreamException();

            // Offset to first IFD from TIFF start
            uint ifdOffset = await ReadUInt32(fs, littleEndian);

            var results = new List<(string, string)>();
            var visitedIfds = new HashSet<uint>();

            await ReadIfdAtOffset(fs, tiffStart, littleEndian, ifdOffset, results, visitedIfds);

            return results;
        }

        private async static Task<uint> ReadUInt32(Stream s, bool little)
        {
            byte[] b = new byte[4];
            if (await s.ReadAsync(b) != 4) throw new EndOfStreamException();
            return little ? BinaryPrimitives.ReadUInt32LittleEndian(b)
                          : BinaryPrimitives.ReadUInt32BigEndian(b);
        }

        private async static Task<ushort> ReadUInt16(Stream s, bool little)
        {
            byte[] b = new byte[2];
            if (await s.ReadAsync(b) != 2) throw new EndOfStreamException();
            return little ? BinaryPrimitives.ReadUInt16LittleEndian(b)
                          : BinaryPrimitives.ReadUInt16BigEndian(b);
        }

        /// <summary>
        /// Reads an IFD at the given offset (relative to TIFF start), collecting textual tags.
        /// Follows the ExifIFDPointer (0x8769) into the Exif SubIFD used by Auto1111.
        /// </summary>
        private async static Task ReadIfdAtOffset(
            Stream fs,
            long tiffStart,
            bool little,
            uint ifdOffset,
            List<(string Key, string Value)> results,
            HashSet<uint> visitedIfds)
        {
            // Guard against loops / corrupt files
            if (visitedIfds.Contains(ifdOffset)) return;
            visitedIfds.Add(ifdOffset);

            long saved = fs.Position;
            fs.Seek(tiffStart + ifdOffset, SeekOrigin.Begin);

            ushort count = await ReadUInt16(fs, little);

            for (int i = 0; i < count; i++)
            {
                ushort tag = await ReadUInt16(fs, little);
                ushort type = await ReadUInt16(fs, little);
                uint numVals = await ReadUInt32(fs, little);
                uint valueOffset = await ReadUInt32(fs, little); // offset or inline value

                // 0x8769 = ExifIFDPointer (LONG, count=1) => points to Exif SubIFD
                if (tag == 0x8769 && type == 4 && numVals == 1)
                {
                    await ReadIfdAtOffset(fs, tiffStart, little, valueOffset, results, visitedIfds);
                    continue;
                }

                string key = TagName(tag);
                if (key != null)
                {
                    string value = await ReadExifValue(fs, tiffStart, type, numVals, valueOffset, little, tag);
                    if (!string.IsNullOrWhiteSpace(value))
                        results.Add((key, value));
                }
            }

            // Optional: there can be a linked "next IFD" after the entries. We ignore it for now,
            // but you could read another uint here and recurse similarly if needed.

            fs.Seek(saved, SeekOrigin.Begin);
        }

        // ----------------------------------------------------------
        // Reading individual EXIF values
        // ----------------------------------------------------------

        private async static Task<string> ReadExifValue(
            Stream fs,
            long tiffStart,
            ushort type,
            uint count,
            uint offset,
            bool little,
            ushort tag)
        {
            long save = fs.Position;

            // If the value doesn't fit in 4 bytes, 'offset' is a pointer into the TIFF data.
            // For ASCII / UNDEFINED strings used here, it's effectively always a pointer.
            long valuePos = tiffStart + offset;
            fs.Seek(valuePos, SeekOrigin.Begin);

            try
            {
                switch (type)
                {
                    case 2: // ASCII string
                        {
                            byte[] b = new byte[count];
                            if (await fs.ReadAsync(b) != b.Length) throw new EndOfStreamException();
                            return b.BytesToString().TrimEnd('\0').Trim();
                        }

                    case 7: // UNDEFINED; often used for UserComment and XP* tags
                        {
                            byte[] b = new byte[count];
                            if (await fs.ReadAsync(b) != b.Length) throw new EndOfStreamException();

                            // UserComment: "ASCII\0\0\0" or "UNICODE\0"
                            if (b.Length > 8)
                            {
                                var prefix = Encoding.ASCII.GetString(b, 0, 8);
                                if (prefix.StartsWith("ASCII"))
                                {
                                    return b.AsSpan(8).ToArray().BytesToString().TrimEnd('\0');
                                }
                                if (prefix.StartsWith("UNICODE"))
                                {
                                    return Encoding.BigEndianUnicode.GetString(b, 8, b.Length - 8).TrimEnd('\0');
                                }
                            }

                            // XP* tags are UTF-16LE blobs
                            if (tag == 0x9C9B || tag == 0x9C9C || tag == 0x9C9D || tag == 0x9C9E)
                                return Encoding.Unicode.GetString(b).TrimEnd('\0');

                            // Fallback – try UTF-8 then ISO-8859-1
                            return b.BytesToString().TrimEnd('\0');
                        }

                    default:
                        return null;
                }
            }
            finally
            {
                fs.Seek(save, SeekOrigin.Begin);
            }
        }

        // ----------------------------------------------------------
        // Tag name mapping – which ones we expose
        // ----------------------------------------------------------

        private static string TagName(ushort tag) => tag switch
        {
            0x010E => "ImageDescription",
            0x010F => "Make",
            0x0110 => "Model",
            0x0131 => "Software",
            0x013B => "Artist",

            // Auto1111 puts the big SD params blob here (inside Exif SubIFD)
            0x9286 => "UserComment",

            // Windows XP UTF-16 tags
            0x9C9B => "XPTitle",
            0x9C9C => "XPComment",
            0x9C9D => "XPAuthor",
            0x9C9E => "XPKeywords",

            _ => null
        };
    }
}
