using StillHere.Application.Features.Notifications;

namespace StillHere.Web.Components.Pages.Settings;

/// <summary>
/// Flat, feature-local, presentation-only shape backing the Add/Edit notification channel form.
/// Carries every field from both channel shapes (webhook and email) plus a plaintext
/// <see cref="Password"/> field. <see cref="Password"/> is never populated from an existing
/// channel's encrypted value -- it is left blank on edit-load, and a blank value on submit means
/// "keep the existing password unchanged" (same convention as the domain edit form's credential
/// fields).
/// </summary>
internal sealed class NotificationChannelViewModel
{
    public NotificationChannelType Type { get; set; } = NotificationChannelType.Webhook;

    public string Name { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public string Url { get; set; } = "";

    public string BodyTemplate { get; set; } = "";

    public string HttpMethod { get; set; } = "POST";

    public string SmtpHost { get; set; } = "";

    public int? SmtpPort { get; set; }

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public string FromAddress { get; set; } = "";

    public string ToAddresses { get; set; } = "";

    public bool TriggerOnIpChange { get; set; } = true;

    public bool TriggerOnFailure { get; set; } = true;

    public bool TriggerOnSuccess { get; set; }
}
