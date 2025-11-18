using Polly;
using Polly.Retry;
using SDMeta;

namespace SDMetaUI
{
    public class RetryingFileLoader(IPngFileLoader inner, ILogger<RetryingFileLoader> logger) : IPngFileLoader
    {
        private static readonly RetryStrategyOptions exponentialRetryOptions = new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
        };

        private static readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
                .AddRetry(exponentialRetryOptions)
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();

        public async Task<PngFile> GetPngFile(string filename)
        {
            try
            {

                return await pipeline.ExecuteAsync(async p => await inner.GetPngFile(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retries failed for {filename}", filename);
            }
            return null;
        }
    }
}
