namespace StillHere.Infrastructure.Persistence.Entities;

internal sealed class ManagedDomain
{
    public int Id { get; set; }

    public required string DomainName { get; set; }

    public required string Host { get; set; }

    public int ProviderCredentialId { get; set; }

    public DnsProviderCredential? ProviderCredential { get; set; }

    public bool Enabled { get; set; } = true;

    public int? PollingIntervalOverrideSeconds { get; set; }

    public string? LastKnownIp { get; set; }

    public DateTime? LastCheckedAtUtc { get; set; }

    public DateTime? LastUpdatedAtUtc { get; set; }

    public DomainCheckStatus LastStatus { get; set; } = DomainCheckStatus.Unknown;

    public DateTime CreatedAtUtc { get; set; }
}
