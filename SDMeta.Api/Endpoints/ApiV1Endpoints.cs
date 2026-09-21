using SDMeta;
using SDMeta.Api.Contracts;
using SDMeta.Api.Services;
using SDMeta.Auto1111;
using SDMeta.Cache;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Text.Json;

namespace SDMeta.Api.Endpoints;

public static class ApiV1Endpoints
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapGet("/health", Health);
        api.MapGet("/images", GetImages);
        api.MapGet("/images/{imageId}", GetImageDetail);
        api.MapDelete("/images/{imageId}", DeleteImage);
        api.MapGet("/images/{imageId}/thumbnail", GetThumbnail);
        api.MapGet("/images/{imageId}/content", GetContent);

        api.MapGet("/models", GetModels);

        api.MapGet("/scans/pending", GetPending);
        api.MapPost("/scans/full", StartFullScan);
        api.MapPost("/scans/partial", StartPartialScan);
        api.MapGet("/scans/{scanId:guid}", GetScan);
        api.MapGet("/scans/{scanId:guid}/events", GetScanEvents);

        api.MapGet("/settings/storage", GetStorage);
        api.MapDelete("/settings/cache/thumbnails", DeleteThumbnails);
        api.MapDelete("/settings/cache/database", DeleteDatabase);

        return app;
    }

    private static IResult Health(IImageFileDataSource dataSource)
    {
        try
        {
            _ = dataSource.GetAllFilenames().Take(1).ToList();
            return Results.Ok(new { status = "ok" });
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiError("health_check_failed", "Health check failed", ex.Message), statusCode: 503);
        }
    }

    private static IResult GetImages(
        string? filter,
        string? model,
        string? modelHash,
        string? sortBy,
        string? groupBy,
        string? cursor,
        int? limit,
        IImageFileDataSource dataSource,
        IImageIdCodec imageIdCodec)
    {
        if (CursorCodec.TryDecode(cursor, out var offset) == false)
        {
            return Results.BadRequest(new ApiError("invalid_cursor", "Cursor is invalid."));
        }

        var pageSize = Math.Clamp(limit ?? 100, 1, 500);

        if (Enum.TryParse<QuerySortBy>(sortBy ?? nameof(QuerySortBy.Newest), ignoreCase: true, out var parsedSortBy) == false)
        {
            return Results.BadRequest(new ApiError("invalid_sortby", $"Unknown sortBy '{sortBy}'."));
        }

        var normalizedGroupBy = (groupBy ?? "none").ToLowerInvariant();
        if (normalizedGroupBy is not ("none" or "prompt"))
        {
            return Results.BadRequest(new ApiError("invalid_groupby", $"Unknown groupBy '{groupBy}'."));
        }

        try
        {
            var modelFilter = (model, modelHash) == (null, null) ? null : new ModelFilter(model, modelHash);
            var query = new QueryParams(filter, modelFilter, parsedSortBy);
            var summaries = dataSource.Query(query).ToList();

            if (normalizedGroupBy == "prompt")
            {
                var grouped = summaries
                    .GroupBy(p => p.FullPromptHash ?? string.Empty)
                    .Select(p => new PromptGroup(
                        p.Key,
                        p.Count(),
                        p.Select(x => ToListItem(x, imageIdCodec)).ToList()))
                    .ToList();

                var page = grouped.Skip(offset).Take(pageSize).ToList();
                var next = offset + pageSize < grouped.Count ? CursorCodec.Encode(offset + pageSize) : null;

                return Results.Ok(new ImageListResponse(null, page, next, grouped.Count));
            }
            else
            {
                var page = summaries.Skip(offset).Take(pageSize).Select(p => ToListItem(p, imageIdCodec)).ToList();
                var next = offset + pageSize < summaries.Count ? CursorCodec.Encode(offset + pageSize) : null;
                return Results.Ok(new ImageListResponse(page, null, next, summaries.Count));
            }
        }
        catch (Exception ex) when (ex.Message.Contains("fts5", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ApiError("invalid_filter_syntax", "The filter syntax is invalid for FTS query."));
        }
    }

    private static IResult GetImageDetail(
        string imageId,
        IImageIdCodec imageIdCodec,
        IImagePathAuthorizer authorizer,
        IImageFileDataSource dataSource,
        IParameterDecoder parameterDecoder)
    {
        if (TryDecodeAuthorizedPath(imageId, imageIdCodec, authorizer, out var fullPath, out var error) == false)
        {
            return error!;
        }

        var image = dataSource.ReadImageFile(fullPath!);
        if (image == null)
        {
            return Results.NotFound(new ApiError("image_not_found", "Image metadata not found."));
        }

        var generationParams = parameterDecoder.GetParameters(image);
        ParsedPrompt? parsedPrompt = null;

        if (generationParams.Prompt != null || generationParams.NegativePrompt != null)
        {
            parsedPrompt = generationParams is Auto1111GenerationParams auto1111
                ? new ParsedPrompt(auto1111.Prompt, auto1111.NegativePrompt, auto1111.Params, auto1111.Warnings)
                : new ParsedPrompt(generationParams.Prompt, generationParams.NegativePrompt, null, null);
        }

        var result = new ImageDetailResponse(
            imageId,
            image.FileName,
            image.LastUpdated.ToUniversalTime(),
            image.Length,
            image.PromptFormat,
            image.Prompt,
            parsedPrompt,
            generationParams.Model,
            generationParams.ModelHash,
            generationParams.PromptHash,
            generationParams.NegativePromptHash,
            image.Exists);

        return Results.Ok(result);
    }

    private static IResult DeleteImage(
        string imageId,
        IImageIdCodec imageIdCodec,
        IImagePathAuthorizer authorizer,
        IImageFileDataSource dataSource,
        IThumbnailService thumbnailService,
        PendingFileChangeQueue pendingFileChangeQueue,
        IFileSystem fileSystem)
    {
        if (TryDecodeAuthorizedPath(imageId, imageIdCodec, authorizer, out var fullPath, out var error) == false)
        {
            return error!;
        }

        var path = fullPath!;

        try
        {
            pendingFileChangeQueue.RegisterRemoval(path);
            thumbnailService.Delete(path);

            if (fileSystem.File.Exists(path))
            {
                fileSystem.File.Delete(path);
            }

            var existing = dataSource.ReadImageFile(path);
            if (existing != null)
            {
                existing.Exists = false;
                dataSource.WriteImageFile(existing);
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Json(new ApiError("delete_failed", "Failed to delete image.", ex.Message), statusCode: 500);
        }
    }

    private static IResult GetThumbnail(
        string imageId,
        int? size,
        IImageIdCodec imageIdCodec,
        IImagePathAuthorizer authorizer,
        IThumbnailService thumbnailService,
        IFileSystem fileSystem,
        HttpResponse response)
    {
        if (size.HasValue && size != ThumbnailService.ThumbnailSize)
        {
            return Results.BadRequest(new ApiError("unsupported_thumbnail_size", $"Only size={ThumbnailService.ThumbnailSize} is currently supported."));
        }

        if (TryDecodeAuthorizedPath(imageId, imageIdCodec, authorizer, out var fullPath, out var error) == false)
        {
            return error!;
        }

        var path = fullPath!;
        if (fileSystem.File.Exists(path) == false)
        {
            return Results.NotFound(new ApiError("image_not_found", "Image file was not found."));
        }

        var fileInfo = fileSystem.FileInfo.New(path);
        var thumbPath = thumbnailService.GetOrGenerateThumbnail(path);
        response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");

        return Results.File(thumbPath, "image/jpeg", enableRangeProcessing: true);
    }

    private static IResult GetContent(
        string imageId,
        IImageIdCodec imageIdCodec,
        IImagePathAuthorizer authorizer,
        IFileSystem fileSystem,
        HttpResponse response)
    {
        if (TryDecodeAuthorizedPath(imageId, imageIdCodec, authorizer, out var fullPath, out var error) == false)
        {
            return error!;
        }

        var path = fullPath!;
        if (fileSystem.File.Exists(path) == false)
        {
            return Results.NotFound(new ApiError("image_not_found", "Image file was not found."));
        }

        var fileInfo = fileSystem.FileInfo.New(path);
        response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");

        var extension = fileSystem.Path.GetExtension(path).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/png",
        };

        return Results.File(path, contentType, enableRangeProcessing: true);
    }

    private static IResult GetModels(IImageFileDataSource dataSource)
    {
        var models = dataSource.GetModelSummaryList()
            .Select(p => new ModelResponseItem(
                p.Model,
                p.ModelHash,
                p.Count,
                (p.ModelHash ?? "<empty>") + " (" + (p.Model ?? "<no name>") + ") [" + p.Count + "]"))
            .ToList();

        return Results.Ok(models);
    }

    private static IResult StartFullScan(ScanJobManager scanJobManager)
    {
        var state = scanJobManager.StartFullScan();
        return Results.Accepted($"/api/v1/scans/{state.ScanId}", new StartScanResponse(state.ScanId));
    }

    private static IResult StartPartialScan(PartialScanRequest request, ScanJobManager scanJobManager)
    {
        var state = scanJobManager.StartPartialScan(request);
        return Results.Accepted($"/api/v1/scans/{state.ScanId}", new StartScanResponse(state.ScanId));
    }

    private static IResult GetPending(PendingFileChangeQueue pendingFileChangeQueue)
    {
        var counts = pendingFileChangeQueue.GetCounts();
        return Results.Ok(new PendingChangesResponse(counts.AddedCount, counts.RemovedCount));
    }

    private static IResult GetScan(Guid scanId, ScanJobManager scanJobManager)
    {
        if (scanJobManager.TryGet(scanId, out var state) == false)
        {
            return Results.NotFound(new ApiError("scan_not_found", "Scan not found."));
        }

        return Results.Ok(state);
    }

    private static async Task<IResult> GetScanEvents(Guid scanId, ScanJobManager scanJobManager, HttpContext context)
    {
        if (scanJobManager.TryGet(scanId, out var initial) == false || initial == null)
        {
            return Results.NotFound(new ApiError("scan_not_found", "Scan not found."));
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        var jsonOptions = context.RequestServices
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value
            .SerializerOptions;

        var revision = -1L;

        while (context.RequestAborted.IsCancellationRequested == false)
        {
            var next = await scanJobManager.WaitForUpdateAsync(scanId, revision, context.RequestAborted);
            if (next == null)
            {
                return Results.Empty;
            }

            if (next.Revision != revision)
            {
                revision = next.Revision;
                var json = JsonSerializer.Serialize(next, jsonOptions);
                await context.Response.WriteAsync($"event: progress\ndata: {json}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }

            if (next.Status is ScanStatus.Completed or ScanStatus.Failed)
            {
                break;
            }
        }

        return Results.Empty;
    }

    private static IResult GetStorage(
        IImageDir imageDir,
        IThumbnailService thumbnailService,
        DbPath dbPath,
        IFileSystem fileSystem)
    {
        var path = dbPath.GetPath();
        long? dbSize = fileSystem.File.Exists(path) ? fileSystem.FileInfo.New(path).Length : null;

        return Results.Ok(new StorageSettingsResponse(
            imageDir.GetPath().ToList(),
            thumbnailService.GetThumbnailDirectory(),
            path,
            dbSize));
    }

    private static IResult DeleteThumbnails(IThumbnailService thumbnailService)
    {
        thumbnailService.DeleteThumbs();
        return Results.NoContent();
    }

    private static IResult DeleteDatabase(IImageFileDataSource dataSource)
    {
        dataSource.Truncate();
        return Results.NoContent();
    }

    private static ImageListItem ToListItem(ImageFileSummary summary, IImageIdCodec imageIdCodec)
    {
        var imageId = imageIdCodec.Encode(summary.FileName);
        return new ImageListItem(
            imageId,
            summary.FileName,
            summary.FullPromptHash,
            $"/api/v1/images/{imageId}/thumbnail",
            $"/api/v1/images/{imageId}/content");
    }

    private static bool TryDecodeAuthorizedPath(
        string imageId,
        IImageIdCodec imageIdCodec,
        IImagePathAuthorizer authorizer,
        out string? fullPath,
        out IResult? error)
    {
        fullPath = null;
        error = null;

        if (imageIdCodec.TryDecode(imageId, out var decoded) == false || string.IsNullOrWhiteSpace(decoded))
        {
            error = Results.BadRequest(new ApiError("invalid_image_id", "Image ID is invalid."));
            return false;
        }

        if (authorizer.IsAuthorized(decoded) == false)
        {
            error = Results.BadRequest(new ApiError("invalid_image_id", "Image path is not within configured image directories."));
            return false;
        }

        fullPath = decoded;
        return true;
    }
}



