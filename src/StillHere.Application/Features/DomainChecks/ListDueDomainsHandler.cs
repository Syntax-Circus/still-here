using StillHere.Application.Features.Domains;
using StillHere.Application.Features.Settings;

namespace StillHere.Application.Features.DomainChecks;

/// <summary>
/// A genuine addition beyond PHASE-06-scheduler.md's two named handlers: the scheduler tick, as an
/// entry point, must not itself coordinate two repository/provider reads (settings + domain
/// listing) per APPLICATION_ARCHITECTURE.md's "entry points must not... coordinate multiple
/// repositories or providers."
/// </summary>
public interface IListDueDomainsHandler
{
    Task<IReadOnlyList<int>> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken);
}

public sealed class ListDueDomainsHandler(
    IManagedDomainRepository managedDomains,
    IGlobalSettingsReader globalSettings) : IListDueDomainsHandler
{
    public async Task<IReadOnlyList<int>> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var defaultIntervalSeconds = await globalSettings.GetDefaultPollingIntervalSecondsAsync(cancellationToken);
        var summaries = await managedDomains.ListEnabledSummariesAsync(cancellationToken);

        return [.. summaries
            .Where(s => DueDomainSelector.IsDue(s.LastCheckedAtUtc, s.PollingIntervalOverrideSeconds, defaultIntervalSeconds, nowUtc))
            .Select(s => s.Id)];
    }
}
