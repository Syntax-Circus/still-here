using Microsoft.EntityFrameworkCore;
using StillHere.Application.Features.Domains;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class ManagedDomainRepository(AppDbContext db) : IManagedDomainRepository
{
    public async Task<ManagedDomainDto> CreateAsync(
        string domainName,
        string host,
        string providerKey,
        string credentialName,
        string encryptedSecretsJson,
        int? pollingIntervalOverrideSeconds,
        CancellationToken cancellationToken)
    {
        var credential = new DnsProviderCredential
        {
            ProviderKey = providerKey,
            Name = credentialName,
            EncryptedSecrets = encryptedSecretsJson,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var domain = new ManagedDomain
        {
            DomainName = domainName,
            Host = host,
            ProviderCredential = credential,
            PollingIntervalOverrideSeconds = pollingIntervalOverrideSeconds,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.ManagedDomains.Add(domain);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(domain, providerKey);
    }

    public async Task<ManagedDomainDto?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        var domain = await db.ManagedDomains
            .Include(d => d.ProviderCredential)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return domain is null ? null : ToDto(domain, domain.ProviderCredential!.ProviderKey);
    }

    public async Task<ManagedDomainDto> UpdateAsync(
        int id,
        string domainName,
        string host,
        bool enabled,
        int? pollingIntervalOverrideSeconds,
        string? newEncryptedSecretsJson,
        CancellationToken cancellationToken)
    {
        var domain = await db.ManagedDomains
            .Include(d => d.ProviderCredential)
            .FirstAsync(d => d.Id == id, cancellationToken);

        domain.DomainName = domainName;
        domain.Host = host;
        domain.Enabled = enabled;
        domain.PollingIntervalOverrideSeconds = pollingIntervalOverrideSeconds;

        if (newEncryptedSecretsJson is not null)
        {
            domain.ProviderCredential!.EncryptedSecrets = newEncryptedSecretsJson;
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(domain, domain.ProviderCredential!.ProviderKey);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var domain = await db.ManagedDomains
            .Include(d => d.ProviderCredential)
            .FirstAsync(d => d.Id == id, cancellationToken);

        db.ManagedDomains.Remove(domain);
        db.DnsProviderCredentials.Remove(domain.ProviderCredential!);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static ManagedDomainDto ToDto(ManagedDomain domain, string providerKey) => new(
        domain.Id,
        domain.DomainName,
        domain.Host,
        providerKey,
        domain.Enabled,
        domain.PollingIntervalOverrideSeconds,
        domain.CreatedAtUtc);
}
