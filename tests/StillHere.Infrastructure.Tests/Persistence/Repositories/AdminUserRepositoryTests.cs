using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.Persistence.Repositories;

public sealed class AdminUserRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly AdminUserRepository _repository;

    public AdminUserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _repository = new AdminUserRepository(_db);
    }

    [Fact]
    public async Task AnyExistsAsync_NoAdmin_ReturnsFalse()
    {
        (await _repository.AnyExistsAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateAsync_ThenAnyExistsAsync_ReturnsTrue()
    {
        await _repository.CreateAsync("admin", "hashed-password", CancellationToken.None);

        (await _repository.AnyExistsAsync(CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_ThenFindByUsernameAsync_RoundTrips()
    {
        var created = await _repository.CreateAsync("admin", "hashed-password", CancellationToken.None);

        var found = await _repository.FindByUsernameAsync("admin", CancellationToken.None);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(created.Id);
        found.Username.ShouldBe("admin");
        found.PasswordHash.ShouldBe("hashed-password");
    }

    [Fact]
    public async Task FindByUsernameAsync_UnknownUsername_ReturnsNull()
    {
        (await _repository.FindByUsernameAsync("nobody", CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_ThenFindByIdAsync_RoundTrips()
    {
        var created = await _repository.CreateAsync("admin", "hashed-password", CancellationToken.None);

        var found = await _repository.FindByIdAsync(created.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.Username.ShouldBe("admin");
    }

    [Fact]
    public async Task UpdatePasswordAsync_ChangesStoredHash()
    {
        var created = await _repository.CreateAsync("admin", "old-hash", CancellationToken.None);

        await _repository.UpdatePasswordAsync(created.Id, "new-hash", CancellationToken.None);

        var found = await _repository.FindByIdAsync(created.Id, CancellationToken.None);
        found.ShouldNotBeNull();
        found.PasswordHash.ShouldBe("new-hash");
    }

    [Fact]
    public async Task UpdateLastLoginAsync_PersistsTimestamp()
    {
        var created = await _repository.CreateAsync("admin", "hashed-password", CancellationToken.None);
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        await _repository.UpdateLastLoginAsync(created.Id, timestamp, CancellationToken.None);

        var reloaded = await _db.AdminUsers.AsNoTracking().FirstAsync(u => u.Id == created.Id, CancellationToken.None);
        reloaded.LastLoginAtUtc.ShouldBe(timestamp);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
