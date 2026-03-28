using System.Text;
using System.IO.Abstractions;
using SDMeta;

namespace SDMeta.Api.Services;

public interface IImageIdCodec
{
    string Encode(string fullPath);
    bool TryDecode(string imageId, out string? fullPath);
}

public sealed class ImageIdCodec : IImageIdCodec
{
    public string Encode(string fullPath)
    {
        var bytes = Encoding.UTF8.GetBytes(fullPath);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public bool TryDecode(string imageId, out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return false;
        }

        try
        {
            var normalized = imageId.Replace('-', '+').Replace('_', '/');
            var padding = 4 - normalized.Length % 4;
            if (padding < 4)
            {
                normalized = normalized + new string('=', padding);
            }

            var decoded = Convert.FromBase64String(normalized);
            fullPath = Encoding.UTF8.GetString(decoded);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public interface IImagePathAuthorizer
{
    bool IsAuthorized(string fullPath);
}

public sealed class ImagePathAuthorizer(IImageDir imageDir) : IImagePathAuthorizer
{
    private readonly string[] _roots = imageDir.GetPath()
        .Select(NormalizeRoot)
        .Where(p => p != null)
        .Select(p => p!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool IsAuthorized(string fullPath)
    {
        if (_roots.Length == 0)
        {
            return false;
        }

        var target = NormalizePath(fullPath);
        return _roots.Any(root => target.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRoot(string root)
    {
        var full = NormalizePath(root);
        if (full.EndsWith(Path.DirectorySeparatorChar))
        {
            return full;
        }
        return full + Path.DirectorySeparatorChar;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
    }
}

public static class CursorCodec
{
    public static string Encode(int offset)
    {
        var bytes = Encoding.UTF8.GetBytes(offset.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out int offset)
    {
        offset = 0;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            var padding = 4 - normalized.Length % 4;
            if (padding < 4)
            {
                normalized += new string('=', padding);
            }

            var bytes = Convert.FromBase64String(normalized);
            var value = Encoding.UTF8.GetString(bytes);
            if (int.TryParse(value, out var parsed) && parsed >= 0)
            {
                offset = parsed;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
