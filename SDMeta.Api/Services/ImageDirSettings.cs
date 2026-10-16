using SDMeta;

namespace SDMeta.Api.Services;

public sealed class ImageDirSettings(IConfiguration configuration) : IImageDir
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private static readonly string[] Keys = ["ImageDir", .. Enumerable.Range(0, 10).Select(p => $"ImageDir:{p}")];

    public IEnumerable<string> GetPath()
    {
        return Keys
            .Select(p => _configuration[p])
            .Where(p => string.IsNullOrWhiteSpace(p) == false)
            .Select(p => p!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
