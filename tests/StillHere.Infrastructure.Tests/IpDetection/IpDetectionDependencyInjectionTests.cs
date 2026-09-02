using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StillHere.Application;
using StillHere.Application.IpDetection;
using StillHere.Infrastructure.IpDetection;
using Xunit;

namespace StillHere.Infrastructure.Tests.IpDetection;

public sealed class IpDetectionDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureAndAddApplication_ResolvesIIpDetectionService()
    {
        using var provider = BuildProvider();
        var ipDetection = provider.GetRequiredService<IIpDetectionService>();

        ipDetection.ShouldNotBeNull();
    }

    [Fact]
    public void AddInfrastructure_RegistersIpDetectionCacheAsSingleton()
    {
        using var provider = BuildProvider();

        using var scopeOne = provider.CreateScope();
        using var scopeTwo = provider.CreateScope();
        var cacheFromScopeOne = scopeOne.ServiceProvider.GetRequiredService<IpDetectionCache>();
        var cacheFromScopeTwo = scopeTwo.ServiceProvider.GetRequiredService<IpDetectionCache>();

        cacheFromScopeOne.ShouldBeSameAs(cacheFromScopeTwo);
    }

    private static ServiceProvider BuildProvider()
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

        return services.BuildServiceProvider();
    }
}
