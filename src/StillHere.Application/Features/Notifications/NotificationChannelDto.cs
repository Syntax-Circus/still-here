namespace StillHere.Application.Features.Notifications;

public sealed record NotificationChannelDto(
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
    string? EncryptedPassword,
    string? FromAddress,
    string? ToAddresses,
    bool TriggerOnIpChange,
    bool TriggerOnFailure,
    bool TriggerOnSuccess);
