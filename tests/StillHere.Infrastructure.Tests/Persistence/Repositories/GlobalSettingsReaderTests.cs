using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Entities;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.Persistence.Repositories;

public sealed class GlobalSettingsReaderTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly GlobalSettingsReader _reader;

    public GlobalSettingsReaderTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _reader = new GlobalSettingsReader(_db);
    }

    [Fact]
    public async Task GetDefaultPollingIntervalSecondsAsync_SeededDefault_Returns300()
    {
        (await _reader.GetDefaultPollingIntervalSecondsAsync(CancellationToken.None)).ShouldBe(300);
    }

    [Fact]
    public async Task GetDefaultPollingIntervalSecondsAsync_MutatedValue_ReturnsMutatedValue()
    {
        var settings = await _db.GlobalSettings.SingleAsync(CancellationToken.None);
        settings.DefaultPollingIntervalSeconds = 120;
        await _db.SaveChangesAsync(CancellationToken.None);

        (await _reader.GetDefaultPollingIntervalSecondsAsync(CancellationToken.None)).ShouldBe(120);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
