using Polly;
using Polly.Retry;
using SDMeta;

namespace SDMetaUI
{
    public class RetryingFileLoader : IPngFileLoader
    {
        private readonly IPngFileLoader inner;
        private readonly ResiliencePipeline pipeline;

        public RetryingFileLoader(IPngFileLoader inner)
        {
            this.inner = inner;

            var exponentialRetryOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
            };

            pipeline = new ResiliencePipelineBuilder()
                .AddRetry(exponentialRetryOptions)
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();
        }

        public async Task<PngFile> GetPngFile(string filename)
        {
            return await pipeline.ExecuteAsync(async p => await inner.GetPngFile(filename));
        }
    }
}
