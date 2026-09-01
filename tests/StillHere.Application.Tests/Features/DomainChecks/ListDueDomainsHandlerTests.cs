using NSubstitute;
using Shouldly;
using StillHere.Application.Features.DomainChecks;
using StillHere.Application.Features.Domains;
using StillHere.Application.Features.Settings;
using Xunit;

namespace StillHere.Application.Tests.Features.DomainChecks;

public sealed class ListDueDomainsHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly IGlobalSettingsReader _globalSettings = Substitute.For<IGlobalSettingsReader>();
    private readonly ListDueDomainsHandler _handler;

    public ListDueDomainsHandlerTests()
    {
        _handler = new ListDueDomainsHandler(_managedDomains, _globalSettings);
    }

    [Fact]
    public async Task HandleAsync_MixedDueAndNotDueDomains_ReturnsOnlyDueIds()
    {
        var now = new DateTime(2026, 1, 1, 0, 10, 0, DateTimeKind.Utc);
        _globalSettings.GetDefaultPollingIntervalSecondsAsync(Arg.Any<CancellationToken>()).Returns(300);
        _managedDomains.ListEnabledSummariesAsync(Arg.Any<CancellationToken>()).Returns(new List<ManagedDomainScheduleSummaryDto>
        {
            new(1, null, null),                                           // never checked -- due
            new(2, null, now.AddSeconds(-100)),                           // 100s ago, default 300s -- not due
            new(3, null, now.AddSeconds(-400)),                           // 400s ago, default 300s -- due
            new(4, PollingIntervalOverrideSeconds: 60, now.AddSeconds(-100)), // override 60s -- due
        });

        var dueIds = await _handler.HandleAsync(now, TestContext.Current.CancellationToken);

        dueIds.ShouldBe([1, 3, 4], ignoreOrder: true);
    }

    [Fact]
    public async Task HandleAsync_CalledOnce_ReadsSettingsOnlyOnceRegardlessOfDomainCount()
    {
        _globalSettings.GetDefaultPollingIntervalSecondsAsync(Arg.Any<CancellationToken>()).Returns(300);
        _managedDomains.ListEnabledSummariesAsync(Arg.Any<CancellationToken>()).Returns(new List<ManagedDomainScheduleSummaryDto>
        {
            new(1, null, null),
            new(2, null, null),
            new(3, null, null),
        });

        await _handler.HandleAsync(DateTime.UtcNow, TestContext.Current.CancellationToken);

        await _globalSettings.Received(1).GetDefaultPollingIntervalSecondsAsync(Arg.Any<CancellationToken>());
    }
}
