using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

namespace UI.API.Configurations
{
    public static class ResilienceConfiguration
    {
        public static void AddResiliencePolicies(this IServiceCollection services) =>
            services.AddSingleton<ResiliencePolicies>();
    }

    /// <summary>
    /// Retry + circuit breaker policies for outbound HTTP calls, shared across the whole project.
    /// Registered as a singleton (see <see cref="ResilienceConfiguration.AddResiliencePolicies"/>) so
    /// every HttpClient that opts in shares the same circuit breaker state — building a fresh policy
    /// per call would reset its failure count every time and the breaker would never trip.
    /// </summary>
    public class ResiliencePolicies
    {
        public IAsyncPolicy<HttpResponseMessage> Retry { get; }
        public IAsyncPolicy<HttpResponseMessage> CircuitBreaker { get; }

        public ResiliencePolicies(ILogger<ResiliencePolicies> logger)
        {
            Retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3,
                    attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                    onRetry: (outcome, delay, attempt, _) =>
                        logger.LogWarning(
                            "Retry {Attempt}/3 after {DelayMs}ms. Reason: {Reason}",
                            attempt, delay.TotalMilliseconds, DescribeFailure(outcome)));

            CircuitBreaker = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDuration) =>
                        logger.LogError(
                            "Circuit breaker opened for {BreakDurationSeconds}s. Reason: {Reason}",
                            breakDuration.TotalSeconds, DescribeFailure(outcome)),
                    onReset: () => logger.LogInformation("Circuit breaker reset; calls resumed."),
                    onHalfOpen: () => logger.LogInformation("Circuit breaker half-open; testing the next call."));
        }

        private static string DescribeFailure(DelegateResult<HttpResponseMessage> outcome) =>
            outcome.Exception?.Message ?? $"HTTP {(int)outcome.Result.StatusCode}";
    }
}
