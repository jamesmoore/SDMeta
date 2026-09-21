using PhotoSauce.MagicScaler;
using SDMeta.Cache;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace SDMeta.Api.Services;

public interface IThumbnailService
{
    string GetOrGenerateThumbnail(string fullName);
    void Delete(string fullName);
    void DeleteThumbs();
    string GetThumbnailDirectory();
}

public sealed class ThumbnailService(IFileSystem fileSystem, DataPath dataPath) : IThumbnailService
{
    public const int ThumbnailSize = 175;

    public string GetOrGenerateThumbnail(string fullName)
    {
        var thumbnailFullName = GetThumbnailFileName(fullName);

        if (fileSystem.File.Exists(thumbnailFullName) == false)
        {
            _ = MagicImageProcessor.ProcessImage(fullName, thumbnailFullName,
                new ProcessImageSettings { Height = ThumbnailSize, Width = ThumbnailSize });
        }

        return thumbnailFullName;
    }

    public void Delete(string fullName)
    {
        var thumbnailFullName = GetThumbnailFileName(fullName);

        if (fileSystem.File.Exists(thumbnailFullName))
        {
            fileSystem.File.Delete(thumbnailFullName);
        }
    }

    public void DeleteThumbs()
    {
        var thumbDir = GetThumbnailDirectory();

        if (fileSystem.Directory.Exists(thumbDir))
        {
            fileSystem.Directory.Delete(thumbDir, recursive: true);
        }
    }

    public string GetThumbnailDirectory()
    {
        return fileSystem.Path.Combine(dataPath.GetPath(), "cache", "thumbs");
    }

    private string GetThumbnailFileName(string fullName)
    {
        var thumbnailName = HashWithSHA256(fullName) + ".jpg";

        var thumbDir = fileSystem.Path.Combine(GetThumbnailDirectory(), thumbnailName[..2]);
        fileSystem.Directory.CreateDirectory(thumbDir);

        return fileSystem.Path.Combine(thumbDir, thumbnailName);
    }

    private static string HashWithSHA256(string value)
    {
        using var hash = SHA256.Create();
        var byteArray = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Base32Encoding.ToString(byteArray);
    }
}

public static class Base32Encoding
{
    public static string ToString(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
        var returnArray = new char[charCount];

        byte nextChar = 0;
        byte bitsRemaining = 5;
        var arrayIndex = 0;

        foreach (byte b in input)
        {
            nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
            returnArray[arrayIndex++] = ValueToChar(nextChar);

            if (bitsRemaining < 4)
            {
                nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                returnArray[arrayIndex++] = ValueToChar(nextChar);
                bitsRemaining += 5;
            }

            bitsRemaining -= 3;
            nextChar = (byte)((b << bitsRemaining) & 31);
        }

        if (arrayIndex != charCount)
        {
            returnArray[arrayIndex++] = ValueToChar(nextChar);
            while (arrayIndex != charCount)
            {
                returnArray[arrayIndex++] = '=';
            }
        }

        return new string(returnArray);
    }

    private static char ValueToChar(byte b)
    {
        if (b < 26)
        {
            return (char)(b + 65);
        }

        if (b < 32)
        {
            return (char)(b + 24);
        }

        throw new ArgumentException("Byte is not a Base32 value.", nameof(b));
    }
}
