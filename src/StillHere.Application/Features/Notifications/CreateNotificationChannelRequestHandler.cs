using StillHere.Application.Security;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Notifications;

public sealed record CreateNotificationChannelRequest(
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

public interface ICreateNotificationChannelRequestHandler
{
    Task<Result<NotificationChannelDto>> HandleAsync(
        CreateNotificationChannelRequest request, CancellationToken cancellationToken);
}

public sealed class CreateNotificationChannelRequestHandler(
    INotificationChannelRepository notificationChannels,
    ISmtpCredentialProtector smtpCredentialProtector) : ICreateNotificationChannelRequestHandler
{
    public async Task<Result<NotificationChannelDto>> HandleAsync(
        CreateNotificationChannelRequest request,
        CancellationToken cancellationToken)
    {
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

        string? encryptedPassword = null;
        if (request.Type == NotificationChannelType.Email && !string.IsNullOrWhiteSpace(request.Password))
        {
            encryptedPassword = smtpCredentialProtector.Protect(request.Password);
        }

        var created = await notificationChannels.CreateAsync(
            request.Type,
            request.Name,
            request.Enabled,
            request.Url,
            request.BodyTemplate,
            httpMethod,
            request.SmtpHost,
            request.SmtpPort,
            request.UseSsl,
            request.Username,
            encryptedPassword,
            request.FromAddress,
            request.ToAddresses,
            request.TriggerOnIpChange,
            request.TriggerOnFailure,
            request.TriggerOnSuccess,
            cancellationToken);

        return Result<NotificationChannelDto>.Success(created);
    }
}
