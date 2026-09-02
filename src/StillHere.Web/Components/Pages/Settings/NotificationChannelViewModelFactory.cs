using StillHere.Application.Features.Notifications;

namespace StillHere.Web.Components.Pages.Settings;

internal static class NotificationChannelViewModelFactory
{
    /// <summary>
    /// Maps a persisted channel to the edit-form view model. <see cref="NotificationChannelViewModel.Password"/>
    /// is intentionally left blank rather than populated from <see cref="NotificationChannelDto.EncryptedPassword"/>
    /// -- the form only sends a new password when the admin is actually rotating it.
    /// </summary>
    public static NotificationChannelViewModel Create(NotificationChannelDto dto) => new()
    {
        Type = dto.Type,
        Name = dto.Name,
        Enabled = dto.Enabled,
        Url = dto.Url ?? "",
        BodyTemplate = dto.BodyTemplate ?? "",
        HttpMethod = dto.HttpMethod ?? "POST",
        SmtpHost = dto.SmtpHost ?? "",
        SmtpPort = dto.SmtpPort,
        UseSsl = dto.UseSsl,
        Username = dto.Username ?? "",
        Password = "",
        FromAddress = dto.FromAddress ?? "",
        ToAddresses = dto.ToAddresses ?? "",
        TriggerOnIpChange = dto.TriggerOnIpChange,
        TriggerOnFailure = dto.TriggerOnFailure,
        TriggerOnSuccess = dto.TriggerOnSuccess,
    };

    public static bool ShowsWebhookFields(NotificationChannelType type) => type == NotificationChannelType.Webhook;

    public static bool ShowsEmailFields(NotificationChannelType type) => type == NotificationChannelType.Email;
}
