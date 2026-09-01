using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Application.Features.AuditLog;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Entities;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.Persistence.Repositories;

public sealed class AuditLogRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly AuditLogRepository _repository;

    public AuditLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _repository = new AuditLogRepository(_db);
    }

    [Fact]
    public async Task QueryAsync_NoEntries_ReturnsEmptyPagedResultWithZeroTotalCount()
    {
        var result = await _repository.QueryAsync(null, null, null, null, null, 1, 25, CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task QueryAsync_FiltersByManagedDomainId_ReturnsOnlyThatDomainsEntries()
    {
        var domainA = await CreateDomainAsync("a.com");
        var domainB = await CreateDomainAsync("b.com");
        await AddEntryAsync(domainA.Id, AuditEventType.CheckOnly, DateTime.UtcNow);
        await AddEntryAsync(domainB.Id, AuditEventType.CheckOnly, DateTime.UtcNow);

        var result = await _repository.QueryAsync(domainA.Id, null, null, null, null, 1, 25, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].ManagedDomainId.ShouldBe(domainA.Id);
    }

    [Fact]
    public async Task QueryAsync_FiltersByEventType_ReturnsOnlyMatchingKind()
    {
        var domain = await CreateDomainAsync("a.com");
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, DateTime.UtcNow);
        await AddEntryAsync(domain.Id, AuditEventType.IpChanged, DateTime.UtcNow);

        var result = await _repository.QueryAsync(null, AuditEventKind.IpChanged, null, null, null, 1, 25, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].EventType.ShouldBe(AuditEventKind.IpChanged);
    }

    [Fact]
    public async Task QueryAsync_FiltersBySuccess_ReturnsOnlyMatchingFlag()
    {
        var domain = await CreateDomainAsync("a.com");
        await AddEntryAsync(domain.Id, AuditEventType.UpdateFailed, DateTime.UtcNow, success: false);
        await AddEntryAsync(domain.Id, AuditEventType.UpdateSucceeded, DateTime.UtcNow, success: true);

        var result = await _repository.QueryAsync(null, null, false, null, null, 1, 25, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Success.ShouldBeFalse();
    }

    [Fact]
    public async Task QueryAsync_FiltersByDateRange_ExcludesEntriesOutsideRange()
    {
        var domain = await CreateDomainAsync("a.com");
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.QueryAsync(
            null, null, null,
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            1, 25, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].TimestampUtc.ShouldBe(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task QueryAsync_OrdersByTimestampDescending()
    {
        var domain = await CreateDomainAsync("a.com");
        var oldest = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newest = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        var middle = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, oldest);
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, newest);
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, middle);

        var result = await _repository.QueryAsync(null, null, null, null, null, 1, 25, CancellationToken.None);

        result.Items.Select(e => e.TimestampUtc).ShouldBe([newest, middle, oldest]);
    }

    [Fact]
    public async Task QueryAsync_Paginates_ReturnsCorrectItemsAndTotalPages()
    {
        var domain = await CreateDomainAsync("a.com");
        for (var i = 0; i < 5; i++)
        {
            await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, new DateTime(2026, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc));
        }

        var page1 = await _repository.QueryAsync(null, null, null, null, null, 1, 2, CancellationToken.None);
        var page2 = await _repository.QueryAsync(null, null, null, null, null, 2, 2, CancellationToken.None);
        var page3 = await _repository.QueryAsync(null, null, null, null, null, 3, 2, CancellationToken.None);

        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(2);
        page3.Items.Count.ShouldBe(1);
        page1.TotalCount.ShouldBe(5);
        page1.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task QueryAsync_ManagedDomainIdSet_PopulatesManagedDomainNameFromJoin()
    {
        var domain = await CreateDomainAsync("example.com");
        await AddEntryAsync(domain.Id, AuditEventType.CheckOnly, DateTime.UtcNow);

        var result = await _repository.QueryAsync(null, null, null, null, null, 1, 25, CancellationToken.None);

        result.Items[0].ManagedDomainName.ShouldBe("example.com");
    }

    [Fact]
    public async Task QueryAsync_ManagedDomainIdNull_LeavesManagedDomainNameNull()
    {
        await AddEntryAsync(managedDomainId: null, AuditEventType.CheckOnly, DateTime.UtcNow);

        var result = await _repository.QueryAsync(null, null, null, null, null, 1, 25, CancellationToken.None);

        result.Items[0].ManagedDomainName.ShouldBeNull();
    }

    private async Task<ManagedDomain> CreateDomainAsync(string domainName)
    {
        var credential = new DnsProviderCredential
        {
            ProviderKey = "namecheap",
            Name = $"{domainName} credential",
            EncryptedSecrets = "encrypted",
            CreatedAtUtc = DateTime.UtcNow,
        };
        var domain = new ManagedDomain
        {
            DomainName = domainName,
            Host = "@",
            ProviderCredential = credential,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.ManagedDomains.Add(domain);
        await _db.SaveChangesAsync(CancellationToken.None);

        return domain;
    }

    private async Task AddEntryAsync(int? managedDomainId, AuditEventType eventType, DateTime timestampUtc, bool success = true)
    {
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ManagedDomainId = managedDomainId,
            TimestampUtc = timestampUtc,
            EventType = eventType,
            Message = "test message",
            Success = success,
        });

        await _db.SaveChangesAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
