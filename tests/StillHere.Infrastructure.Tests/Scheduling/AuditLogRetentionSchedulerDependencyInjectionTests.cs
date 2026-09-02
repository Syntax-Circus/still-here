using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using StillHere.Application;
using StillHere.Infrastructure.Scheduling;
using Xunit;

namespace StillHere.Infrastructure.Tests.Scheduling;

public sealed class AuditLogRetentionSchedulerDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersAuditLogRetentionSchedulerAsHostedService()
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
        var hostedServices = provider.GetServices<IHostedService>();

        hostedServices.ShouldContain(s => s is AuditLogRetentionScheduler);
    }
}
