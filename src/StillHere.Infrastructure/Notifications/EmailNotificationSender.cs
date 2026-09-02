using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StillHere.Application.Features.Notifications;
using StillHere.Application.Security;
using SyntaxCircus.Email;

namespace StillHere.Infrastructure.Notifications;

/// <summary>
/// SMTP settings are per-channel (host, credentials, TLS) rather than one static app-wide profile,
/// so a fresh <see cref="SmtpEmailSender"/> is constructed for every send rather than shared as a
/// singleton -- this rules out the package's <c>AddSmtpEmailSender(configuration)</c> DI extension,
/// which wires exactly one static SMTP profile. Matches <see cref="WebhookNotificationSender"/>'s
/// "never let an exception escape <see cref="SendAsync"/>" convention: third-party SMTP client
/// failures are exactly the kind of boundary that convention exists for.
/// </summary>
internal sealed class EmailNotificationSender(
    ISmtpCredentialProtector smtpCredentialProtector, ILoggerFactory loggerFactory) : INotificationSender
{
    private const int DefaultSmtpPort = 587;

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public async Task<NotificationSendResult> SendAsync(
        NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var decryptedPassword = string.IsNullOrEmpty(channel.EncryptedPassword)
                ? string.Empty
                : smtpCredentialProtector.Unprotect(channel.EncryptedPassword);

            var options = new SmtpOptions
            {
                Host = channel.SmtpHost!,
                Port = channel.SmtpPort ?? DefaultSmtpPort,
                Username = channel.Username ?? string.Empty,
                Password = decryptedPassword,
                UseStartTls = channel.UseSsl,
                DefaultFrom = channel.FromAddress!,
            };

            var sender = new SmtpEmailSender(
                Options.Create(options), loggerFactory.CreateLogger<SmtpEmailSender>(), new MailKitSmtpClientFactory());

            var subject = $"still-here: {context.Status} — {context.DomainName}";
            var body = BuildBody(context);

            var message = new EmailMessage(
                channel.ToAddresses!, subject, body, false, channel.FromAddress!, [], [], body, "", []);

            await sender.SendAsync(message, cancellationToken);

            return NotificationSendResult.Succeeded("Email notification sent.");
        }
        catch (Exception ex)
        {
            // SmtpEmailSender/MailKit is a third-party SMTP client boundary -- matches
            // NamecheapDnsProvider's and WebhookNotificationSender's catch-all-at-the-boundary
            // convention: never let an exception escape SendAsync.
            return NotificationSendResult.Failed($"Email send failed: {ex.Message}");
        }
    }

    private static string BuildBody(NotificationEventContext context) =>
        $"""
        Domain: {context.DomainName}
        Status: {context.Status}
        Old IP: {context.OldIp ?? "(none)"}
        New IP: {context.NewIp ?? "(none)"}

        {context.Message}

        -- still-here
        """;
}
