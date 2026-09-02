namespace StillHere.Application.Features.DnsProviders;

public interface IDnsProviderRegistry
{
    IReadOnlyList<IDnsProvider> Providers { get; }

    IDnsProvider GetByKey(string providerKey);
}
