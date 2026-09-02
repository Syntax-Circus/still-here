using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using StillHere.Infrastructure.Security;
using Xunit;

namespace StillHere.Infrastructure.Tests.Domains;

/// <summary>
/// Exercises the real handler + real repository + real credential protector end to end, then reads
/// the raw database column to confirm the stored value is genuinely encrypted -- not just trusting
/// that the pieces were wired together correctly in isolation.
/// </summary>
public sealed class ManagedDomainCredentialEncryptionTests : IDisposable
{
    private const string PlaintextPassword = "super-secret-ddns-password";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly string _keysDirectory = Path.Combine(Path.GetTempPath(), $"stillhere-test-keys-{Guid.NewGuid():N}");
    private readonly AppDbContext _db;

    public ManagedDomainCredentialEncryptionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddManagedDomain_StoresEncryptedSecrets_NeverPlaintext()
    {
        var namecheap = Substitute.For<IDnsProvider>();
        namecheap.ProviderKey.Returns("namecheap");
        namecheap.CredentialFields.Returns((IReadOnlyList<ProviderCredentialField>)
            [new ProviderCredentialField("Password", "Dynamic DNS Password", IsSecret: true)]);

        var dnsProviders = Substitute.For<IDnsProviderRegistry>();
        dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[namecheap]);

        var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(_keysDirectory));
        var credentialProtector = new CredentialProtector(dataProtectionProvider);
        var repository = new ManagedDomainRepository(_db);
        var handler = new AddManagedDomainRequestHandler(repository, dnsProviders, credentialProtector);

        var result = await handler.HandleAsync(
            new AddManagedDomainRequest(
                "example.com", "@", "namecheap", "example.com credential",
                new Dictionary<string, string> { ["Password"] = PlaintextPassword },
                PollingIntervalOverrideSeconds: null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var storedSecret = await _db.DnsProviderCredentials.AsNoTracking()
            .Select(c => c.EncryptedSecrets)
            .FirstAsync(CancellationToken.None);

        storedSecret.ShouldNotContain(PlaintextPassword);
        storedSecret.ShouldNotBeNullOrWhiteSpace();

        // Round-trip via a fresh protector sharing the same key ring, matching how a
        // later phase (scheduler) will decrypt this value at read time.
        var reOpenedProtector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(_keysDirectory)));
        reOpenedProtector.Unprotect(storedSecret).ShouldContain(PlaintextPassword);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);

        if (Directory.Exists(_keysDirectory))
        {
            Directory.Delete(_keysDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
