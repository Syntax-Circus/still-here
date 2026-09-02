using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using Xunit;

namespace StillHere.Infrastructure.Tests.Persistence.Repositories;

public sealed class NotificationChannelRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly NotificationChannelRepository _repository;

    public NotificationChannelRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _repository = new NotificationChannelRepository(_db);
    }

    private Task<NotificationChannelDto> CreateWebhookAsync(bool enabled = true) =>
        _repository.CreateAsync(
            NotificationChannelType.Webhook, "webhook channel", enabled,
            url: "https://example.com/hook", bodyTemplate: "{\"text\":\"{{message}}\"}", httpMethod: "POST",
            smtpHost: null, smtpPort: null, useSsl: false, username: null, encryptedPassword: null,
            fromAddress: null, toAddresses: null,
            triggerOnIpChange: true, triggerOnFailure: true, triggerOnSuccess: false,
            CancellationToken.None);

    private Task<NotificationChannelDto> CreateEmailAsync(bool enabled = true, string? encryptedPassword = "original-encrypted") =>
        _repository.CreateAsync(
            NotificationChannelType.Email, "email channel", enabled,
            url: null, bodyTemplate: null, httpMethod: null,
            smtpHost: "smtp.example.com", smtpPort: 587, useSsl: true, username: "smtp-user", encryptedPassword: encryptedPassword,
            fromAddress: "from@example.com", toAddresses: "to@example.com",
            triggerOnIpChange: false, triggerOnFailure: true, triggerOnSuccess: true,
            CancellationToken.None);

    [Fact]
    public async Task CreateAsync_ThenFindByIdAsync_RoundTrips_Webhook()
    {
        var created = await CreateWebhookAsync();

        var found = await _repository.FindByIdAsync(created.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.Type.ShouldBe(NotificationChannelType.Webhook);
        found.Name.ShouldBe("webhook channel");
        found.Enabled.ShouldBeTrue();
        found.Url.ShouldBe("https://example.com/hook");
        found.BodyTemplate.ShouldBe("{\"text\":\"{{message}}\"}");
        found.HttpMethod.ShouldBe("POST");
        found.TriggerOnIpChange.ShouldBeTrue();
        found.TriggerOnFailure.ShouldBeTrue();
        found.TriggerOnSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateAsync_ThenFindByIdAsync_RoundTrips_Email()
    {
        var created = await CreateEmailAsync();

        var found = await _repository.FindByIdAsync(created.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.Type.ShouldBe(NotificationChannelType.Email);
        found.Name.ShouldBe("email channel");
        found.SmtpHost.ShouldBe("smtp.example.com");
        found.SmtpPort.ShouldBe(587);
        found.UseSsl.ShouldBeTrue();
        found.Username.ShouldBe("smtp-user");
        found.EncryptedPassword.ShouldBe("original-encrypted");
        found.FromAddress.ShouldBe("from@example.com");
        found.ToAddresses.ShouldBe("to@example.com");
        found.TriggerOnIpChange.ShouldBeFalse();
        found.TriggerOnFailure.ShouldBeTrue();
        found.TriggerOnSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FindByIdAsync_UnknownId_ReturnsNull()
    {
        (await _repository.FindByIdAsync(999, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAllAsync_ReturnsAllChannelsRegardlessOfEnabled()
    {
        var webhook = await CreateWebhookAsync(enabled: true);
        var email = await CreateEmailAsync(enabled: false);

        var all = await _repository.ListAllAsync(CancellationToken.None);

        all.Count.ShouldBe(2);
        all.ShouldContain(c => c.Id == webhook.Id);
        all.ShouldContain(c => c.Id == email.Id);
    }

    [Fact]
    public async Task ListEnabledAsync_ExcludesDisabledChannels()
    {
        var enabledChannel = await CreateWebhookAsync(enabled: true);
        await CreateEmailAsync(enabled: false);

        var enabled = await _repository.ListEnabledAsync(CancellationToken.None);

        enabled.Count.ShouldBe(1);
        enabled[0].Id.ShouldBe(enabledChannel.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithoutNewPassword_LeavesEncryptedPasswordUnchanged()
    {
        var created = await CreateEmailAsync(encryptedPassword: "original-encrypted");

        var updated = await _repository.UpdateAsync(
            created.Id, "renamed channel", enabled: false,
            url: null, bodyTemplate: null, httpMethod: null,
            smtpHost: "smtp2.example.com", smtpPort: 465, useSsl: false, username: "new-user", newEncryptedPassword: null,
            fromAddress: "from2@example.com", toAddresses: "to2@example.com",
            triggerOnIpChange: true, triggerOnFailure: false, triggerOnSuccess: false,
            CancellationToken.None);

        updated.Name.ShouldBe("renamed channel");
        updated.Enabled.ShouldBeFalse();
        updated.SmtpHost.ShouldBe("smtp2.example.com");
        updated.SmtpPort.ShouldBe(465);
        updated.UseSsl.ShouldBeFalse();
        updated.Username.ShouldBe("new-user");
        updated.FromAddress.ShouldBe("from2@example.com");
        updated.ToAddresses.ShouldBe("to2@example.com");
        updated.TriggerOnIpChange.ShouldBeTrue();
        updated.TriggerOnFailure.ShouldBeFalse();
        updated.TriggerOnSuccess.ShouldBeFalse();
        updated.EncryptedPassword.ShouldBe("original-encrypted");
    }

    [Fact]
    public async Task UpdateAsync_WithNewPassword_OverwritesEncryptedPassword()
    {
        var created = await CreateEmailAsync(encryptedPassword: "original-encrypted");

        var updated = await _repository.UpdateAsync(
            created.Id, "email channel", enabled: true,
            url: null, bodyTemplate: null, httpMethod: null,
            smtpHost: "smtp.example.com", smtpPort: 587, useSsl: true, username: "smtp-user", newEncryptedPassword: "rotated-encrypted",
            fromAddress: "from@example.com", toAddresses: "to@example.com",
            triggerOnIpChange: false, triggerOnFailure: true, triggerOnSuccess: true,
            CancellationToken.None);

        updated.EncryptedPassword.ShouldBe("rotated-encrypted");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var created = await CreateWebhookAsync();

        await _repository.DeleteAsync(created.Id, CancellationToken.None);

        (await _repository.FindByIdAsync(created.Id, CancellationToken.None)).ShouldBeNull();
        (await _db.NotificationChannels.AsNoTracking().AnyAsync(CancellationToken.None)).ShouldBeFalse();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
