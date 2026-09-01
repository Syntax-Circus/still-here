namespace StillHere.Application.Features.DnsProviders;

public sealed class DnsProviderRegistry : IDnsProviderRegistry
{
    private readonly IReadOnlyList<IDnsProvider> _providers;

    public DnsProviderRegistry(IEnumerable<IDnsProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = [.. providers];
    }

    public IReadOnlyList<IDnsProvider> Providers => _providers;

    public IDnsProvider GetByKey(string providerKey)
    {
        foreach (var provider in _providers)
        {
            if (provider.ProviderKey == providerKey)
            {
                return provider;
            }
        }

        throw new InvalidOperationException($"No DNS provider registered for key '{providerKey}'.");
    }
}
