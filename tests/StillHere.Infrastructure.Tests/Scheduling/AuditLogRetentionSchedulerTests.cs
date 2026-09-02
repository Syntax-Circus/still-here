using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using StillHere.Application;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Entities;
using StillHere.Infrastructure.Scheduling;
using Xunit;

namespace StillHere.Infrastructure.Tests.Scheduling;

public sealed class AuditLogRetentionSchedulerTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _provider;

    public AuditLogRetentionSchedulerTests()
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

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Tick_RetentionSetWithExpiredEntries_PrunesOnlyExpiredEntries()
    {
        await using (var setupScope = _provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(CancellationToken.None);

            var settings = await db.GlobalSettings.SingleAsync(CancellationToken.None);
            settings.AuditLogRetentionDays = 10;

            db.AuditLogEntries.AddRange(
                new AuditLogEntry
                {
                    TimestampUtc = DateTime.UtcNow.AddDays(-20),
                    EventType = AuditEventType.CheckOnly,
                    Message = "old",
                    Success = true,
                },
                new AuditLogEntry
                {
                    TimestampUtc = DateTime.UtcNow.AddDays(-1),
                    EventType = AuditEventType.CheckOnly,
                    Message = "recent",
                    Success = true,
                });

            await db.SaveChangesAsync(CancellationToken.None);
        }

        var scheduler = new AuditLogRetentionScheduler(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            TimeSpan.FromDays(1),
            NullLogger<AuditLogRetentionScheduler>.Instance);

        await scheduler.RunTickAsync(CancellationToken.None);

        await using var assertScope = _provider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remaining = await assertDb.AuditLogEntries.AsNoTracking().ToListAsync(CancellationToken.None);

        remaining.Count.ShouldBe(1);
        remaining[0].Message.ShouldBe("recent");
    }

    public void Dispose()
    {
        _provider.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
