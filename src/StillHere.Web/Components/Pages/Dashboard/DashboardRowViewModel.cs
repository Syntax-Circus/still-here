using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.Dashboard;

internal sealed record DashboardRowViewModel(
    int Id,
    string DomainName,
    string Host,
    string ProviderKey,
    bool Enabled,
    int? PollingIntervalOverrideSeconds,
    string LastKnownIpDisplay,
    string LastCheckedDisplay,
    ManagedDomainStatus Status);
