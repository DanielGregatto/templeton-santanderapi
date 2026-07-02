using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Services.Features.Stories.Queries.GetBestStories;
using Services.Infrastructure.HackerNews.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Unit.Tests.Services.Queries.Stories;

public class GetBestStoriesQueryHandlerTests
{
    private readonly Mock<IHackerNewsClient> _mockClient = new();
    private readonly GetBestStoriesQueryValidator _validator = new();

    private GetBestStoriesQueryHandler CreateHandler() =>
        new(_mockClient.Object, _validator, NullLogger<GetBestStoriesQueryHandler>.Instance);

    private void SetupStories(params HackerNewsItem[] items)
    {
        _mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Select(i => i.Id).ToList());

        foreach (var item in items)
        {
            var captured = item;
            _mockClient.Setup(c => c.GetItemAsync(captured.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(captured);
        }
    }

    private static HackerNewsItem Story(int id, int score, string? url = "https://example.com", bool dead = false, bool deleted = false) => new()
    {
        Id = id,
        Type = "story",
        Title = $"Story {id}",
        Url = url,
        By = "author",
        Time = 1570887781,
        Score = score,
        Descendants = 5,
        Dead = dead,
        Deleted = deleted
    };

    [Fact]
    public async Task Handle_ReturnsStoriesOrderedByScoreDescending()
    {
        SetupStories(Story(1, score: 50), Story(2, score: 200), Story(3, score: 100));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 3 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Select(s => s.Score).Should().Equal(200, 100, 50);
    }

    [Fact]
    public async Task Handle_WhenNIsLessThanAvailable_ReturnsOnlyTopN()
    {
        SetupStories(Story(1, score: 50), Story(2, score: 200), Story(3, score: 100));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 2 }, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data.Select(s => s.Score).Should().Equal(200, 100);
    }

    [Fact]
    public async Task Handle_WhenNExceedsAvailableStories_ReturnsAllOfThem()
    {
        SetupStories(Story(1, score: 50), Story(2, score: 200));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 50 }, CancellationToken.None);

        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ExcludesDeadAndDeletedStories()
    {
        SetupStories(
            Story(1, score: 200, dead: true),
            Story(2, score: 150, deleted: true),
            Story(3, score: 100));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 10 }, CancellationToken.None);

        result.Data.Should().ContainSingle().Which.Score.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WhenUrlIsMissing_FallsBackToHackerNewsDiscussionLink()
    {
        SetupStories(Story(42, score: 100, url: null));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 1 }, CancellationToken.None);

        result.Data.Single().Uri.Should().Be("https://news.ycombinator.com/item?id=42");
    }

    [Fact]
    public async Task Handle_MapsFieldsToStoryResult()
    {
        SetupStories(Story(1, score: 321));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 1 }, CancellationToken.None);

        var story = result.Data.Single();
        story.Title.Should().Be("Story 1");
        story.Uri.Should().Be("https://example.com");
        story.PostedBy.Should().Be("author");
        story.Score.Should().Be(321);
        story.CommentCount.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_WhenNIsNotPositive_ReturnsValidationFailure(int n)
    {
        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = n }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _mockClient.Verify(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOneItemFetchFails_ExcludesItButStillReturnsTheRest()
    {
        var okStory = Story(1, score: 100);
        _mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { okStory.Id, 999 });
        _mockClient.Setup(c => c.GetItemAsync(okStory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(okStory);
        _mockClient.Setup(c => c.GetItemAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated upstream failure"));

        var result = await CreateHandler().Handle(new GetBestStoriesQuery { N = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle().Which.Score.Should().Be(100);
    }
}
