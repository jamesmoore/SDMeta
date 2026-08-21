using SDMeta.Cache;

namespace SDMeta.Api.Contracts;

public sealed record ApiError(string Code, string Message, object? Details = null);

public sealed record ImageListItem(
    string ImageId,
    string FileName,
    string? FullPromptHash,
    string ThumbnailUrl,
    string ContentUrl);

public sealed record PromptGroup(
    string FullPromptHash,
    int Count,
    IReadOnlyList<ImageListItem> Items);

public sealed record ImageListResponse(
    IReadOnlyList<ImageListItem>? Items,
    IReadOnlyList<PromptGroup>? Groups,
    string? NextCursor,
    int TotalApprox);

public sealed record ParsedPrompt(
    string? Positive,
    string? Negative,
    string? Parameters,
    string? Warnings);

public sealed record ImageDetailResponse(
    string ImageId,
    string FileName,
    DateTime LastUpdatedUtc,
    long LengthBytes,
    PromptFormat PromptFormat,
    string? PromptRaw,
    ParsedPrompt? PromptParsed,
    string? Model,
    string? ModelHash,
    string? PromptHash,
    string? NegativePromptHash,
    bool Exists);

public sealed record ModelResponseItem(
    string? Model,
    string? ModelHash,
    int Count,
    string Label);

public sealed record StartScanResponse(Guid ScanId);

public sealed record PartialScanRequest(
    IReadOnlyList<string>? Added,
    IReadOnlyList<string>? Removed,
    bool UsePendingWatcherQueue = false);

public enum ScanStatus
{
    Queued,
    Running,
    Completed,
    Failed,
}

public sealed record ScanStateResponse(
    Guid ScanId,
    string Type,
    ScanStatus Status,
    float Progress,
    int AddedCount,
    int RemovedCount,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? Error,
    long Revision);

public sealed record PendingChangesResponse(int AddedCount, int RemovedCount);

public sealed record StorageSettingsResponse(
    IReadOnlyList<string> ImageDirs,
    string ThumbnailDir,
    string DbPath,
    long? DbSizeBytes);
