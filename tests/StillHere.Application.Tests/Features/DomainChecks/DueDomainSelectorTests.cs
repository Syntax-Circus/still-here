using Shouldly;
using StillHere.Application.Features.DomainChecks;
using Xunit;

namespace StillHere.Application.Tests.Features.DomainChecks;

public sealed class DueDomainSelectorTests
{
    [Fact]
    public void IsDue_NeverChecked_ReturnsTrue()
    {
        DueDomainSelector.IsDue(
            lastCheckedAtUtc: null,
            pollingIntervalOverrideSeconds: null,
            defaultPollingIntervalSeconds: 300,
            nowUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ShouldBeTrue();
    }

    [Fact]
    public void IsDue_IntervalNotYetElapsed_ReturnsFalse()
    {
        var lastChecked = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = lastChecked.AddSeconds(200);

        DueDomainSelector.IsDue(lastChecked, pollingIntervalOverrideSeconds: null, defaultPollingIntervalSeconds: 300, now)
            .ShouldBeFalse();
    }

    [Fact]
    public void IsDue_IntervalElapsed_ReturnsTrue()
    {
        var lastChecked = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = lastChecked.AddSeconds(300);

        DueDomainSelector.IsDue(lastChecked, pollingIntervalOverrideSeconds: null, defaultPollingIntervalSeconds: 300, now)
            .ShouldBeTrue();
    }

    [Fact]
    public void IsDue_OverrideShorterThanDefault_BecomesDueSoonerThanDefaultWould()
    {
        var lastChecked = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = lastChecked.AddSeconds(100);

        DueDomainSelector.IsDue(lastChecked, pollingIntervalOverrideSeconds: 60, defaultPollingIntervalSeconds: 300, now)
            .ShouldBeTrue();
    }

    [Fact]
    public void IsDue_OverrideLongerThanDefault_StaysNotDueLongerThanDefaultWould()
    {
        var lastChecked = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = lastChecked.AddSeconds(400);

        DueDomainSelector.IsDue(lastChecked, pollingIntervalOverrideSeconds: 600, defaultPollingIntervalSeconds: 300, now)
            .ShouldBeFalse();
    }
}
