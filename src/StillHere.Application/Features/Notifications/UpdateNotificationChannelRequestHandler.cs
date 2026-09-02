using StillHere.Application.Security;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Notifications;

/// <summary>
/// <paramref name="Password"/> is optional: a blank or <see langword="null"/> value means "leave
/// the stored SMTP password unchanged" -- the edit form never pre-fills a decrypted password, so a
/// resubmission is only expected when the admin is actually rotating it.
/// </summary>
public sealed record UpdateNotificationChannelRequest(
    int Id,
    NotificationChannelType Type,
    string Name,
    bool Enabled,
    string? Url,
    string? BodyTemplate,
    string? HttpMethod,
    string? SmtpHost,
    int? SmtpPort,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? ToAddresses,
    bool TriggerOnIpChange,
    bool TriggerOnFailure,
    bool TriggerOnSuccess);

public interface IUpdateNotificationChannelRequestHandler
{
    Task<Result<NotificationChannelDto>> HandleAsync(
        UpdateNotificationChannelRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateNotificationChannelRequestHandler(
    INotificationChannelRepository notificationChannels,
    ISmtpCredentialProtector smtpCredentialProtector) : IUpdateNotificationChannelRequestHandler
{
    public async Task<Result<NotificationChannelDto>> HandleAsync(
        UpdateNotificationChannelRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await notificationChannels.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result<NotificationChannelDto>.Failure(new ResultError(
                "notification-channel-not-found", "Notification channel not found.", ResultErrorKind.NotFound));
        }

        if (request.Type != existing.Type)
        {
            return Result<NotificationChannelDto>.Failure(new ResultError(
                "channel-type-immutable",
                "A notification channel's type cannot be changed after creation.",
                ResultErrorKind.Validation,
                nameof(request.Type)));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<NotificationChannelDto>.Failure(new ResultError(
                "name-required", "A name is required.", ResultErrorKind.Validation, nameof(request.Name)));
        }

        var httpMethod = request.HttpMethod;

        if (request.Type == NotificationChannelType.Webhook)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return Result<NotificationChannelDto>.Failure(new ResultError(
                    "url-required", "A URL is required.", ResultErrorKind.Validation, nameof(request.Url)));
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                httpMethod = "POST";
            }
        }

        if (request.Type == NotificationChannelType.Email)
        {
            if (string.IsNullOrWhiteSpace(request.SmtpHost))
            {
                return Result<NotificationChannelDto>.Failure(new ResultError(
                    "smtp-host-required", "An SMTP host is required.", ResultErrorKind.Validation, nameof(request.SmtpHost)));
            }

            if (request.SmtpPort is not > 0)
            {
                return Result<NotificationChannelDto>.Failure(new ResultError(
                    "smtp-port-required", "An SMTP port is required.", ResultErrorKind.Validation, nameof(request.SmtpPort)));
            }

            if (string.IsNullOrWhiteSpace(request.FromAddress))
            {
                return Result<NotificationChannelDto>.Failure(new ResultError(
                    "from-address-required", "A from address is required.", ResultErrorKind.Validation, nameof(request.FromAddress)));
            }

            if (string.IsNullOrWhiteSpace(request.ToAddresses))
            {
                return Result<NotificationChannelDto>.Failure(new ResultError(
                    "to-addresses-required", "A to address is required.", ResultErrorKind.Validation, nameof(request.ToAddresses)));
            }
        }

        string? newEncryptedPassword = null;
        if (request.Type == NotificationChannelType.Email && !string.IsNullOrWhiteSpace(request.Password))
        {
            newEncryptedPassword = smtpCredentialProtector.Protect(request.Password);
        }

        var updated = await notificationChannels.UpdateAsync(
            request.Id,
            request.Name,
            request.Enabled,
            request.Url,
            request.BodyTemplate,
            httpMethod,
            request.SmtpHost,
            request.SmtpPort,
            request.UseSsl,
            request.Username,
            newEncryptedPassword,
            request.FromAddress,
            request.ToAddresses,
            request.TriggerOnIpChange,
            request.TriggerOnFailure,
            request.TriggerOnSuccess,
            cancellationToken);

        return Result<NotificationChannelDto>.Success(updated);
    }
}
