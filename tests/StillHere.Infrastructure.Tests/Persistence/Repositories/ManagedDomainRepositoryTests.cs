using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Application.Features.Domains;
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

    [Fact]
    public async Task ListEnabledSummariesAsync_ExcludesDisabledDomains()
    {
        var enabled = await _repository.CreateAsync(
            "enabled.com", "@", "namecheap", "cred1", "encrypted1", 600, CancellationToken.None);
        var toDisable = await _repository.CreateAsync(
            "disabled.com", "@", "namecheap", "cred2", "encrypted2", null, CancellationToken.None);
        await _repository.UpdateAsync(
            toDisable.Id, "disabled.com", "@", enabled: false, null, newEncryptedSecretsJson: null, CancellationToken.None);

        var summaries = await _repository.ListEnabledSummariesAsync(CancellationToken.None);

        summaries.Count.ShouldBe(1);
        summaries[0].Id.ShouldBe(enabled.Id);
        summaries[0].PollingIntervalOverrideSeconds.ShouldBe(600);
        summaries[0].LastCheckedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task FindForCheckAsync_ReturnsEncryptedSecretsAndLastKnownIp()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "encrypted-secret", null, CancellationToken.None);

        var detail = await _repository.FindForCheckAsync(created.Id, CancellationToken.None);

        detail.ShouldNotBeNull();
        detail.DomainName.ShouldBe("example.com");
        detail.ProviderKey.ShouldBe("namecheap");
        detail.EncryptedSecrets.ShouldBe("encrypted-secret");
        detail.LastKnownIp.ShouldBeNull();
    }

    [Fact]
    public async Task FindForCheckAsync_UnknownId_ReturnsNull()
    {
        (await _repository.FindForCheckAsync(999, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task RecordCheckResultAsync_Updated_SetsLastKnownIpAndLastUpdatedAtUtc()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "encrypted", null, CancellationToken.None);
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        await _repository.RecordCheckResultAsync(
            created.Id, DomainCheckOutcomeKind.Updated, "1.2.3.4", timestamp, CancellationToken.None);

        var domain = await _db.ManagedDomains.AsNoTracking().SingleAsync(CancellationToken.None);
        domain.LastKnownIp.ShouldBe("1.2.3.4");
        domain.LastCheckedAtUtc.ShouldBe(timestamp);
        domain.LastUpdatedAtUtc.ShouldBe(timestamp);
    }

    [Fact]
    public async Task RecordCheckResultAsync_UpdateFailed_LeavesLastKnownIpUnchanged()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "encrypted", null, CancellationToken.None);
        var firstTimestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await _repository.RecordCheckResultAsync(
            created.Id, DomainCheckOutcomeKind.Updated, "1.2.3.4", firstTimestamp, CancellationToken.None);

        var secondTimestamp = firstTimestamp.AddMinutes(5);
        await _repository.RecordCheckResultAsync(
            created.Id, DomainCheckOutcomeKind.UpdateFailed, newLastKnownIp: null, secondTimestamp, CancellationToken.None);

        var domain = await _db.ManagedDomains.AsNoTracking().SingleAsync(CancellationToken.None);
        domain.LastKnownIp.ShouldBe("1.2.3.4");
        domain.LastCheckedAtUtc.ShouldBe(secondTimestamp);
        domain.LastUpdatedAtUtc.ShouldBe(firstTimestamp);
    }

    [Fact]
    public async Task RecordCheckResultAsync_Unchanged_UpdatesLastCheckedAtUtcOnly()
    {
        var created = await _repository.CreateAsync(
            "example.com", "@", "namecheap", "cred", "encrypted", null, CancellationToken.None);
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        await _repository.RecordCheckResultAsync(
            created.Id, DomainCheckOutcomeKind.Unchanged, newLastKnownIp: null, timestamp, CancellationToken.None);

        var domain = await _db.ManagedDomains.AsNoTracking().SingleAsync(CancellationToken.None);
        domain.LastKnownIp.ShouldBeNull();
        domain.LastCheckedAtUtc.ShouldBe(timestamp);
        domain.LastUpdatedAtUtc.ShouldBeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
