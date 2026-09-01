using Shouldly;
using StillHere.Application.IpDetection;
using StillHere.Infrastructure.IpDetection;
using Xunit;

namespace StillHere.Infrastructure.Tests.IpDetection;

public sealed class IpDetectionCacheTests
{
    [Fact]
    public async Task GetOrDetectAsync_CalledMultipleTimesWithinTtl_InvokesFactoryOnce()
    {
        var cache = new IpDetectionCache(TimeSpan.FromSeconds(30));
        var callCount = 0;
        Task<IpDetectionResult> Factory()
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(IpDetectionResult.Succeeded("1.2.3.4"));
        }

        var first = await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);
        var second = await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);

        first.IpAddress.ShouldBe("1.2.3.4");
        second.IpAddress.ShouldBe("1.2.3.4");
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrDetectAsync_CalledConcurrently_InvokesFactoryOnce()
    {
        var cache = new IpDetectionCache(TimeSpan.FromSeconds(30));
        var callCount = 0;
        async Task<IpDetectionResult> Factory()
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50, TestContext.Current.CancellationToken);
            return IpDetectionResult.Succeeded("5.6.7.8");
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken)));

        results.ShouldAllBe(r => r.IpAddress == "5.6.7.8");
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrDetectAsync_AfterExpiry_InvokesFactoryAgain()
    {
        var cache = new IpDetectionCache(TimeSpan.FromMilliseconds(50));
        var callCount = 0;
        Task<IpDetectionResult> Factory()
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(IpDetectionResult.Succeeded("1.1.1.1"));
        }

        await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);

        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetOrDetectAsync_FailedDetection_IsNotCached()
    {
        var cache = new IpDetectionCache(TimeSpan.FromSeconds(30));
        var callCount = 0;
        Task<IpDetectionResult> Factory()
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(IpDetectionResult.Failed("all services down"));
        }

        var first = await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);
        var second = await cache.GetOrDetectAsync(Factory, TestContext.Current.CancellationToken);

        first.Success.ShouldBeFalse();
        second.Success.ShouldBeFalse();
        callCount.ShouldBe(2);
    }
}
