namespace StillHere.Application.Features.DomainChecks;

/// <summary>
/// Public (not internal like <c>CredentialFieldValidator</c>) because <c>StillHere.Application</c>
/// has no <c>InternalsVisibleTo</c> entry, and this pure function needs direct unit test coverage.
/// </summary>
public static class DueDomainSelector
{
    public static bool IsDue(
        DateTime? lastCheckedAtUtc,
        int? pollingIntervalOverrideSeconds,
        int defaultPollingIntervalSeconds,
        DateTime nowUtc)
    {
        if (lastCheckedAtUtc is null)
        {
            return true;
        }

        var effectiveIntervalSeconds = pollingIntervalOverrideSeconds ?? defaultPollingIntervalSeconds;
        return nowUtc >= lastCheckedAtUtc.Value.AddSeconds(effectiveIntervalSeconds);
    }
}
