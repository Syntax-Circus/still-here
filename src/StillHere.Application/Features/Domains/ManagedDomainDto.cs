namespace StillHere.Application.Features.Domains;

public sealed record ManagedDomainDto(
    int Id,
    string DomainName,
    string Host,
    string ProviderKey,
    bool Enabled,
    int? PollingIntervalOverrideSeconds,
    DateTime CreatedAtUtc);
