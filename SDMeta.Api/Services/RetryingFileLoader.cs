using Polly;
using Polly.Retry;
using SDMeta;
using System.IO.Abstractions;

namespace SDMeta.Api.Services;

public sealed class RetryingFileLoader(IImageFileLoader inner, ILogger<RetryingFileLoader> logger) : IImageFileLoader
{
    private static readonly RetryStrategyOptions ExponentialRetryOptions = new()
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
    };

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(ExponentialRetryOptions)
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    public async Task<ImageFile?> GetImageFile(IFileInfo fileInfo)
    {
        try
        {
            return await Pipeline.ExecuteAsync(async _ => await inner.GetImageFile(fileInfo));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Retries failed for {filename}", fileInfo.FullName);
            return null;
        }
    }
}
