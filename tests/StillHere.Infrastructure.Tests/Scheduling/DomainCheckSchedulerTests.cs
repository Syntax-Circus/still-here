using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using StillHere.Application;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Application.IpDetection;
using StillHere.Application.Security;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Entities;
using StillHere.Infrastructure.Persistence.Repositories;
using StillHere.Infrastructure.Scheduling;
using Xunit;

namespace StillHere.Infrastructure.Tests.Scheduling;

public sealed class DomainCheckSchedulerTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly string _keysDirectory = Path.Combine(Path.GetTempPath(), $"stillhere-test-keys-{Guid.NewGuid():N}");

    [Fact]
    public async Task Tick_AgainstFreshUnmigratedDatabase_MigratesSchemaAndCompletesWithoutThrowing()
    {
        var provider = BuildContainer(configureIpDetectionAndDnsProviders: false);
        var scheduler = new DomainCheckScheduler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeSpan.FromSeconds(30),
            NullLogger<DomainCheckScheduler>.Instance);

        // Deliberately no EnsureCreated()/MigrateAsync() beforehand -- the tick must migrate itself.
        await scheduler.RunTickAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var applied = await db.Database.GetAppliedMigrationsAsync(CancellationToken.None);
        var all = db.Database.GetMigrations();
        applied.ShouldBe(all, ignoreOrder: true);
    }

    [Fact]
    public async Task Tick_WithOneDueDomain_WritesAuditEntryAndUpdatesManagedDomain()
    {
        const string plaintextPassword = "ddns-password";

        var provider = BuildContainer(configureIpDetectionAndDnsProviders: true);

        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(CancellationToken.None);

            var credentialProtector = setupScope.ServiceProvider.GetRequiredService<ICredentialProtector>();
            var secretsJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["Password"] = plaintextPassword });
            var encryptedSecrets = credentialProtector.Protect(secretsJson);

            var repository = setupScope.ServiceProvider.GetRequiredService<IManagedDomainRepository>();
            await repository.CreateAsync(
                "example.com", "@", "namecheap", "example.com credential", encryptedSecrets,
                pollingIntervalOverrideSeconds: null, CancellationToken.None);
        }

        var scheduler = new DomainCheckScheduler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeSpan.FromSeconds(30),
            NullLogger<DomainCheckScheduler>.Instance);

        await scheduler.RunTickAsync(CancellationToken.None);

        await using var assertScope = provider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entries = await assertDb.AuditLogEntries.AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync(CancellationToken.None);
        entries.Count.ShouldBe(2);
        entries[0].EventType.ShouldBe(AuditEventType.IpChanged);
        entries[0].NewIp.ShouldBe("9.9.9.9");
        entries[1].EventType.ShouldBe(AuditEventType.UpdateSucceeded);
        entries[1].Success.ShouldBeTrue();

        var domain = await assertDb.ManagedDomains.AsNoTracking().SingleAsync(CancellationToken.None);
        domain.LastKnownIp.ShouldBe("9.9.9.9");
        domain.LastStatus.ShouldBe(DomainCheckStatus.Ok);
        domain.LastCheckedAtUtc.ShouldNotBeNull();
        domain.LastUpdatedAtUtc.ShouldNotBeNull();
    }

    private ServiceProvider BuildContainer(bool configureIpDetectionAndDnsProviders)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddApplication();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(_keysDirectory));

        if (configureIpDetectionAndDnsProviders)
        {
            var ipDetection = Substitute.For<IIpDetectionService>();
            ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("9.9.9.9"));
            ipDetection.CompareProviderReportedIp(Arg.Any<string>(), Arg.Any<string?>()).Returns(ProviderIpComparisonOutcome.Match);
            services.AddSingleton(ipDetection);

            var namecheap = Substitute.For<IDnsProvider>();
            namecheap.ProviderKey.Returns("namecheap");
            namecheap.UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>())
                .Returns(DnsUpdateResult.Succeeded("9.9.9.9", "Updated."));

            var dnsProviders = Substitute.For<IDnsProviderRegistry>();
            dnsProviders.GetByKey("namecheap").Returns(namecheap);
            services.AddSingleton(dnsProviders);
        }

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);

        if (Directory.Exists(_keysDirectory))
        {
            Directory.Delete(_keysDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
