using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Infrastructure.Notifications;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;
using Entities = StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Tests.Notifications;

/// <summary>
/// FR-22: notification send failures must go to the app log only, never the audit log.
/// <see cref="NotificationDispatcher"/> has no dependency on any audit-log abstraction, so it is
/// structurally incapable of writing to the audit log -- this test proves that even a
/// consistently-failing sender never causes an audit-log row to appear.
/// </summary>
public sealed class NotificationDispatcherAuditIsolationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly NotificationDispatcher _dispatcher;

    public NotificationDispatcherAuditIsolationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _db.NotificationChannels.Add(new Entities.NotificationChannel
        {
            Type = Entities.NotificationChannelType.Webhook,
            Name = "Always-failing webhook",
            Enabled = true,
            Url = "https://example.invalid/hook",
            TriggerOnIpChange = true,
            TriggerOnFailure = true,
            TriggerOnSuccess = true,
        });
        _db.SaveChanges();

        var channelRepository = new NotificationChannelRepository(_db);

        var failingSender = Substitute.For<INotificationSender>();
        failingSender.ChannelType.Returns(NotificationChannelType.Webhook);
        failingSender
            .SendAsync(Arg.Any<NotificationChannelDto>(), Arg.Any<NotificationEventContext>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Simulated sender failure."));

        var senderRegistry = Substitute.For<INotificationSenderRegistry>();
        senderRegistry.GetByType(NotificationChannelType.Webhook).Returns(failingSender);

        _dispatcher = new NotificationDispatcher(channelRepository, senderRegistry, NullLogger<NotificationDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_SenderAlwaysThrows_NeverThrowsAndNeverWritesToAuditLog()
    {
        var context = new NotificationEventContext("example.com", "1.1.1.1", "2.2.2.2", "IpChanged", "IP changed.");

        await Should.NotThrowAsync(() => _dispatcher.DispatchAsync(NotificationTrigger.IpChange, context, TestContext.Current.CancellationToken));
        await Should.NotThrowAsync(() => _dispatcher.DispatchAsync(NotificationTrigger.Success, context, TestContext.Current.CancellationToken));
        await Should.NotThrowAsync(() => _dispatcher.DispatchAsync(NotificationTrigger.Failure, context, TestContext.Current.CancellationToken));

        var auditLogRowCount = await _db.AuditLogEntries.CountAsync(TestContext.Current.CancellationToken);
        auditLogRowCount.ShouldBe(0);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
