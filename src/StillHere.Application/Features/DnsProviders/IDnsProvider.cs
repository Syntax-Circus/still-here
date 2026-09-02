namespace StillHere.Application.Features.DnsProviders;

public interface IDnsProvider
{
    string ProviderKey { get; }

    string DisplayName { get; }

    IReadOnlyList<ProviderCredentialField> CredentialFields { get; }

    Task<DnsUpdateResult> UpdateAsync(DnsUpdateRequest request, CancellationToken cancellationToken);
}
