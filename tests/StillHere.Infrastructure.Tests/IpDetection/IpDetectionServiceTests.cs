using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Application.IpDetection;
using StillHere.Infrastructure.IpDetection;
using StillHere.Infrastructure.Persistence;
using Xunit;

namespace StillHere.Infrastructure.Tests.IpDetection;

public sealed class IpDetectionServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;

    public IpDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task DetectCurrentIpAsync_FirstServiceSucceeds_ReturnsItsIpWithoutCallingOthers()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.Host switch
        {
            "ifconfig.me" => TextResponse("1.2.3.4"),
            _ => throw new InvalidOperationException($"Unexpected call to {request.RequestUri}"),
        });
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.IpAddress.ShouldBe("1.2.3.4");
        handler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task DetectCurrentIpAsync_FirstServiceFails_FallsBackToSecond()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.Host switch
        {
            "ifconfig.me" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            "api.ipify.org" => TextResponse("5.6.7.8"),
            _ => throw new InvalidOperationException($"Unexpected call to {request.RequestUri}"),
        });
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.IpAddress.ShouldBe("5.6.7.8");
        handler.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task DetectCurrentIpAsync_FirstTwoServicesFail_FallsBackToThird()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.Host switch
        {
            "ifconfig.me" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            "api.ipify.org" => throw new HttpRequestException("simulated network failure"),
            "icanhazip.com" => TextResponse("9.8.7.6"),
            _ => throw new InvalidOperationException($"Unexpected call to {request.RequestUri}"),
        });
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.IpAddress.ShouldBe("9.8.7.6");
        handler.CallCount.ShouldBe(3);
    }

    [Fact]
    public async Task DetectCurrentIpAsync_AllServicesFail_ReturnsFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.IpAddress.ShouldBeNull();
        handler.CallCount.ShouldBe(3);
    }

    [Fact]
    public async Task DetectCurrentIpAsync_ServiceReturnsNonIpBody_FallsBackToNext()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.Host switch
        {
            "ifconfig.me" => TextResponse("<html>not an ip</html>"),
            "api.ipify.org" => TextResponse("4.3.2.1"),
            _ => throw new InvalidOperationException($"Unexpected call to {request.RequestUri}"),
        });
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.IpAddress.ShouldBe("4.3.2.1");
    }

    [Fact]
    public async Task DetectCurrentIpAsync_ServiceThrowsHttpRequestException_FallsBackToNext()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.Host switch
        {
            "ifconfig.me" => throw new HttpRequestException("simulated network failure"),
            "api.ipify.org" => TextResponse("2.2.2.2"),
            _ => throw new InvalidOperationException($"Unexpected call to {request.RequestUri}"),
        });
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.IpAddress.ShouldBe("2.2.2.2");
    }

    [Fact]
    public async Task DetectCurrentIpAsync_EmptyConfiguredServiceList_ReturnsFailureWithoutHttpCall()
    {
        await SetExternalIpCheckServicesAsync("[]");
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        handler.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DetectCurrentIpAsync_MalformedJsonConfiguration_ReturnsFailureWithoutHttpCall()
    {
        await SetExternalIpCheckServicesAsync("not json");
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var service = CreateService(handler);

        var result = await service.DetectCurrentIpAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        handler.CallCount.ShouldBe(0);
    }

    [Fact]
    public void CompareProviderReportedIp_MatchingIp_ReturnsMatch()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));

        service.CompareProviderReportedIp("1.2.3.4", "1.2.3.4").ShouldBe(ProviderIpComparisonOutcome.Match);
    }

    [Fact]
    public void CompareProviderReportedIp_DifferentIp_ReturnsMismatch()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));

        service.CompareProviderReportedIp("1.2.3.4", "5.6.7.8").ShouldBe(ProviderIpComparisonOutcome.Mismatch);
    }

    [Fact]
    public void CompareProviderReportedIp_ProviderDidNotReportIp_ReturnsNotReported()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));

        service.CompareProviderReportedIp("1.2.3.4", null).ShouldBe(ProviderIpComparisonOutcome.NotReported);
    }

    [Fact]
    public void CompareProviderReportedIp_WhitespaceProviderReportedIp_ReturnsNotReported()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));

        service.CompareProviderReportedIp("1.2.3.4", "   ").ShouldBe(ProviderIpComparisonOutcome.NotReported);
    }

    private async Task SetExternalIpCheckServicesAsync(string json)
    {
        var settings = await _db.GlobalSettings.SingleAsync(TestContext.Current.CancellationToken);
        settings.ExternalIpCheckServices = json;
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private IpDetectionService CreateService(HttpMessageHandler handler) =>
        new(new StubHttpClientFactory(handler), _db, new IpDetectionCache());

    private static HttpResponseMessage TextResponse(string body) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body)) };

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responder(request));
        }
    }
}
