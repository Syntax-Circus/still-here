using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StillHere.Application;
using StillHere.Application.IpDetection;
using Xunit;

namespace StillHere.Infrastructure.Tests.IpDetection;

public sealed class IpDetectionDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureAndAddApplication_ResolvesIIpDetectionService()
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
        var ipDetection = provider.GetRequiredService<IIpDetectionService>();

        ipDetection.ShouldNotBeNull();
    }
}
