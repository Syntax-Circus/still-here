using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StillHere.Application;
using StillHere.Application.Features.DnsProviders;
using Xunit;

namespace StillHere.Infrastructure.Tests.DnsProviders;

public sealed class DnsProviderDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureAndAddApplication_ResolvesNamecheapProviderByKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Never actually opened -- only DI registration/resolution is exercised here.
                ["ConnectionStrings:Default"] = "Data Source=:memory:",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IDnsProviderRegistry>();

        var namecheap = registry.GetByKey("namecheap");

        namecheap.DisplayName.ShouldBe("Namecheap");
        registry.Providers.ShouldContain(p => p.ProviderKey == "namecheap");
    }
}
