using FluentAssertions;
using Services.Infrastructure.HackerNews.Models;
using System;
using Xunit;

namespace Unit.Tests.Infrastructure.HackerNews;

public class HackerNewsItemExtensionsTests
{
    private static HackerNewsItem Story(string? url = "https://example.com") => new()
    {
        Id = 21233041,
        Type = "story",
        Title = "A uBlock Origin update was rejected from the Chrome Web Store",
        Url = url,
        By = "ismaildonmez",
        Time = 1570887781,
        Score = 1716,
        Descendants = 572
    };

    [Fact]
    public void ToStoryResult_MapsAllFields()
    {
        var result = Story().ToStoryResult();

        result.Title.Should().Be("A uBlock Origin update was rejected from the Chrome Web Store");
        result.Uri.Should().Be("https://example.com");
        result.PostedBy.Should().Be("ismaildonmez");
        result.Time.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1570887781));
        result.Score.Should().Be(1716);
        result.CommentCount.Should().Be(572);
    }

    [Fact]
    public void ToStoryResult_WhenUrlIsNull_FallsBackToHackerNewsDiscussionLink()
    {
        var result = Story(url: null).ToStoryResult();

        result.Uri.Should().Be("https://news.ycombinator.com/item?id=21233041");
    }
}
