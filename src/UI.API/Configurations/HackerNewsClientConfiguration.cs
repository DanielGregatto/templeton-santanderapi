using Domain.Configs;
using Microsoft.Extensions.Options;
using Services.Infrastructure.HackerNews;
using Services.Interfaces;

namespace UI.API.Configurations
{
    public static class HackerNewsClientConfiguration
    {
        public static void AddHackerNewsClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<HackerNewsConfig>(configuration.GetSection("HackerNews"));

            // Shared across every inbound request, caps how many calls to Hacker News can be in
            // flight at once.
            services.AddSingleton<HackerNewsRequestThrottle>();

            services.AddHttpClient<IHackerNewsClient, HackerNewsClient>((sp, client) =>
            {
                var hackerNewsConfig = sp.GetRequiredService<IOptions<HackerNewsConfig>>().Value;
                client.BaseAddress = new Uri(hackerNewsConfig.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler((sp, _) => sp.GetRequiredService<ResiliencePolicies>().Retry)
            .AddPolicyHandler((sp, _) => sp.GetRequiredService<ResiliencePolicies>().CircuitBreaker);
        }
    }
}
