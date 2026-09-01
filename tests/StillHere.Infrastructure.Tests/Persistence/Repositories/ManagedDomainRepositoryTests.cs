using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.Persistence.Repositories;

public sealed class ManagedDomainRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly ManagedDomainRepository _repository;

    public ManagedDomainRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _repository = new ManagedDomainRepository(_db);
    }

    [Fact]
    public async Task CreateAsync_ThenFindByIdAsync_RoundTrips()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "example.com credential", "encrypted-blob", 600, CancellationToken.None);

        var found = await _repository.FindByIdAsync(created.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.DomainName.ShouldBe("example.com");
        found.Host.ShouldBe("@");
        found.ProviderKey.ShouldBe("namecheap");
        found.PollingIntervalOverrideSeconds.ShouldBe(600);
        found.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task FindByIdAsync_UnknownId_ReturnsNull()
    {
        (await _repository.FindByIdAsync(999, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithoutNewSecrets_UpdatesDomainFieldsOnly()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "original-encrypted", null, CancellationToken.None);

        var updated = await _repository.UpdateAsync(
            created.Id, "example.org", "www", false, 900, newEncryptedSecretsJson: null, CancellationToken.None);

        updated.DomainName.ShouldBe("example.org");
        updated.Host.ShouldBe("www");
        updated.Enabled.ShouldBeFalse();
        updated.PollingIntervalOverrideSeconds.ShouldBe(900);

        var storedSecret = await _db.DnsProviderCredentials.AsNoTracking()
            .Select(c => c.EncryptedSecrets)
            .FirstAsync(CancellationToken.None);
        storedSecret.ShouldBe("original-encrypted");
    }

    [Fact]
    public async Task UpdateAsync_WithNewSecrets_ReplacesEncryptedSecrets()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "original-encrypted", null, CancellationToken.None);

        await _repository.UpdateAsync(
            created.Id, "example.com", "@", true, null, newEncryptedSecretsJson: "rotated-encrypted", CancellationToken.None);

        var storedSecret = await _db.DnsProviderCredentials.AsNoTracking()
            .Select(c => c.EncryptedSecrets)
            .FirstAsync(CancellationToken.None);
        storedSecret.ShouldBe("rotated-encrypted");
    }

    [Fact]
    public async Task DeleteAsync_RemovesBothDomainAndCredentialRows()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "encrypted", null, CancellationToken.None);

        await _repository.DeleteAsync(created.Id, CancellationToken.None);

        (await _db.ManagedDomains.AsNoTracking().AnyAsync(CancellationToken.None)).ShouldBeFalse();
        (await _db.DnsProviderCredentials.AsNoTracking().AnyAsync(CancellationToken.None)).ShouldBeFalse();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
