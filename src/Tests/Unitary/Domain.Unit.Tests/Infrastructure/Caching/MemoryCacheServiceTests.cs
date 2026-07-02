using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Infrastructure.Caching;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Unit.Tests.Infrastructure.Caching;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests()
    {
        _sut = new MemoryCacheService(_memoryCache, NullLogger<MemoryCacheService>.Instance);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCalledRepeatedlyWithinTtl_OnlyInvokesFactoryOnce()
    {
        var callCount = 0;
        Task<int> Factory()
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(42);
        }

        var first = await _sut.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1));
        var second = await _sut.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1));

        first.Should().Be(42);
        second.Should().Be(42);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenManyConcurrentCallsMissTheCache_OnlyInvokesFactoryOnce()
    {
        var callCount = 0;
        async Task<int> Factory()
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50); // simulate a slow upstream call
            return 7;
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => _sut.GetOrCreateAsync("concurrent-key", Factory, TimeSpan.FromMinutes(1))));

        results.Should().AllBeEquivalentTo(7);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_UsesIndependentCacheEntriesPerKey()
    {
        var a = await _sut.GetOrCreateAsync("a", () => Task.FromResult(1), TimeSpan.FromMinutes(1));
        var b = await _sut.GetOrCreateAsync("b", () => Task.FromResult(2), TimeSpan.FromMinutes(1));

        a.Should().Be(1);
        b.Should().Be(2);
    }

    public void Dispose() => _memoryCache.Dispose();
}
