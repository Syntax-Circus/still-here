namespace StillHere.Application.Features.Domains;

public sealed record ManagedDomainSummaryDto(
    int Id,
    string DomainName,
    string Host,
    string ProviderKey,
    bool Enabled,
    int? PollingIntervalOverrideSeconds,
    string? LastKnownIp,
    DateTime? LastCheckedAtUtc,
    DateTime? LastUpdatedAtUtc,
    ManagedDomainStatus Status);
