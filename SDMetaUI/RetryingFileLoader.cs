using Polly;
using Polly.Retry;
using SDMeta;

namespace SDMetaUI
{
    public class RetryingFileLoader(IPngFileLoader inner) : IPngFileLoader
    {
        public PngFile GetPngFile(string filename)
        {
            var exponentialRetryOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<IOException>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
            };

            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(exponentialRetryOptions) 
                .AddTimeout(TimeSpan.FromSeconds(10)) 
                .Build();

            return pipeline.Execute(p => inner.GetPngFile(filename));
        }
    }
}
