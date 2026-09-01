using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Application.Features.AuditLog;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.AuditLog;

public sealed class AuditLogWriterTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly AuditLogWriter _writer;

    public AuditLogWriterTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _writer = new AuditLogWriter(_db);
    }

    [Theory]
    [InlineData(AuditEventKind.CheckOnly)]
    [InlineData(AuditEventKind.IpChanged)]
    [InlineData(AuditEventKind.UpdateFailed)]
    [InlineData(AuditEventKind.UpdateSucceeded)]
    public async Task WriteAsync_EachEventKind_RoundTripsCorrectly(AuditEventKind kind)
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _writer.WriteAsync(
            new WriteAuditLogEntryRequest(
                ManagedDomainId: null, kind, OldIp: "1.2.3.4", NewIp: "5.6.7.8",
                Message: "test message", Success: kind != AuditEventKind.UpdateFailed, timestamp),
            CancellationToken.None);

        var entry = await _db.AuditLogEntries.AsNoTracking().SingleAsync(CancellationToken.None);
        entry.EventType.ToString().ShouldBe(kind.ToString());
        entry.OldIp.ShouldBe("1.2.3.4");
        entry.NewIp.ShouldBe("5.6.7.8");
        entry.Message.ShouldBe("test message");
        entry.TimestampUtc.ShouldBe(timestamp);
    }

    [Fact]
    public async Task WriteAsync_NullManagedDomainId_PersistsAsNull()
    {
        await _writer.WriteAsync(
            new WriteAuditLogEntryRequest(
                ManagedDomainId: null, AuditEventKind.CheckOnly, OldIp: null, NewIp: null,
                Message: "no domain", Success: true, DateTime.UtcNow),
            CancellationToken.None);

        var entry = await _db.AuditLogEntries.AsNoTracking().SingleAsync(CancellationToken.None);
        entry.ManagedDomainId.ShouldBeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
