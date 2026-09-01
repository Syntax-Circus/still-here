namespace StillHere.Infrastructure.Persistence.Entities;

internal sealed class DnsProviderCredential
{
    public int Id { get; set; }

    public required string ProviderKey { get; set; }

    public required string Name { get; set; }

    /// <summary>JSON blob of provider-specific secret fields, encrypted at rest via SyntaxCircus.Credentials.</summary>
    public required string EncryptedSecrets { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ManagedDomain> ManagedDomains { get; } = [];
}
