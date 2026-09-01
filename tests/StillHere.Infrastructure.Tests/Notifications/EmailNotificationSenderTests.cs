using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Application.Security;
using StillHere.Infrastructure.Notifications;
using Xunit;

namespace StillHere.Infrastructure.Tests.Notifications;

/// <summary>
/// <see cref="SyntaxCircus.Email.SmtpEmailSender"/> is constructed directly (per the task's
/// per-channel-SMTP-config requirement, not via the package's DI extension), and it opens a real
/// MailKit connection with no injectable seam short of a live/fake SMTP server -- so these tests
/// exercise only what's verifiable without a real SMTP connection: that the failure path never
/// throws and surfaces as a failed <see cref="NotificationSendResult"/>, and that the password is
/// decrypted via <see cref="ISmtpCredentialProtector"/> before the (doomed) send is attempted. The
/// success path is not covered here -- see the task report for why.
///
/// The connection target is loopback on a port nothing listens on, so the underlying TCP connect
/// fails fast (refused) rather than timing out; <see cref="SyntaxCircus.Email.SmtpOptions.MaxRetryAttempts"/>
/// defaults to 3 with exponential backoff (2s, then 4s), so each of these tests takes roughly 6+
/// seconds -- that retry behavior is the package's, not something this class can override per-send.
/// </summary>
public sealed class EmailNotificationSenderTests
{
    private static readonly NotificationEventContext Context =
        new("example.com", "1.2.3.4", "5.6.7.8", "Success", "IP changed");

    [Fact]
    public async Task SendAsync_UnreachableSmtpHost_ReturnsFailedResultWithoutThrowing()
    {
        var protector = Substitute.For<ISmtpCredentialProtector>();
        protector.Unprotect(Arg.Any<string>()).Returns("decrypted-password");
        var sender = new EmailNotificationSender(protector, NullLoggerFactory.Instance);
        var channel = CreateChannel(encryptedPassword: "cipher-text");

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendAsync_NonNullEncryptedPassword_CallsUnprotectWithChannelValue()
    {
        var protector = Substitute.For<ISmtpCredentialProtector>();
        protector.Unprotect(Arg.Any<string>()).Returns("decrypted-password");
        var sender = new EmailNotificationSender(protector, NullLoggerFactory.Instance);
        var channel = CreateChannel(encryptedPassword: "cipher-text");

        await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        protector.Received(1).Unprotect("cipher-text");
    }

    [Fact]
    public async Task SendAsync_NullEncryptedPassword_DoesNotCallUnprotect()
    {
        var protector = Substitute.For<ISmtpCredentialProtector>();
        var sender = new EmailNotificationSender(protector, NullLoggerFactory.Instance);
        var channel = CreateChannel(encryptedPassword: null);

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        protector.DidNotReceive().Unprotect(Arg.Any<string>());
        result.Success.ShouldBeFalse();
    }

    private static NotificationChannelDto CreateChannel(string? encryptedPassword) =>
        new(
            Id: 1,
            Type: NotificationChannelType.Email,
            Name: "Test Email",
            Enabled: true,
            Url: null,
            BodyTemplate: null,
            HttpMethod: null,
            SmtpHost: "127.0.0.1",
            SmtpPort: 1,
            UseSsl: false,
            Username: "user@example.com",
            EncryptedPassword: encryptedPassword,
            FromAddress: "still-here@example.com",
            ToAddresses: "admin@example.com",
            TriggerOnIpChange: true,
            TriggerOnFailure: true,
            TriggerOnSuccess: true);
}
