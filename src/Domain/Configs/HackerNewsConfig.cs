namespace Domain.Configs
{
    public class HackerNewsConfig
    {
        public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";
        public int BestStoriesCacheSeconds { get; set; } = 60;
        public int ItemCacheSeconds { get; set; } = 300;
        public int MaxConcurrentUpstreamRequests { get; set; } = 20;
    }
}
