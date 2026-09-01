using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.Dashboard;

internal static class DashboardRowViewModelFactory
{
    public static DashboardRowViewModel Create(ManagedDomainSummaryDto dto) => new(
        dto.Id,
        dto.DomainName,
        dto.Host,
        dto.ProviderKey,
        dto.Enabled,
        dto.PollingIntervalOverrideSeconds,
        dto.LastKnownIp ?? "—",
        dto.LastCheckedAtUtc?.ToString("u") ?? "Never checked",
        dto.Status);
}
