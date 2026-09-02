using Shouldly;
using StillHere.Application.Features.DnsProviders;
using Xunit;

namespace StillHere.Application.Tests.Features.DnsProviders;

public sealed class DnsProviderRegistryTests
{
    [Fact]
    public void GetByKey_KnownKey_ReturnsMatchingProvider()
    {
        var namecheap = new FakeDnsProvider("namecheap");
        var cloudflare = new FakeDnsProvider("cloudflare");
        var registry = new DnsProviderRegistry([namecheap, cloudflare]);

        registry.GetByKey("cloudflare").ShouldBeSameAs(cloudflare);
    }

    [Fact]
    public void GetByKey_UnknownKey_Throws()
    {
        var registry = new DnsProviderRegistry([new FakeDnsProvider("namecheap")]);

        Should.Throw<InvalidOperationException>(() => registry.GetByKey("unknown"));
    }

    [Fact]
    public void Providers_ListsEveryRegisteredProvider()
    {
        var namecheap = new FakeDnsProvider("namecheap");
        var cloudflare = new FakeDnsProvider("cloudflare");
        var registry = new DnsProviderRegistry([namecheap, cloudflare]);

        registry.Providers.ShouldBe([namecheap, cloudflare]);
    }

    private sealed class FakeDnsProvider(string providerKey) : IDnsProvider
    {
        public string ProviderKey => providerKey;

        public string DisplayName => providerKey;

        public IReadOnlyList<ProviderCredentialField> CredentialFields => [];

        public Task<DnsUpdateResult> UpdateAsync(DnsUpdateRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not needed for registry tests.");
    }
}
