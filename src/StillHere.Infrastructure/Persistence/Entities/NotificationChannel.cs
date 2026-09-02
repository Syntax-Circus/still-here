namespace StillHere.Infrastructure.Persistence.Entities;

internal sealed class NotificationChannel
{
    public int Id { get; set; }

    public NotificationChannelType Type { get; set; }

    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;

    // Webhook fields
    public string? Url { get; set; }

    public string? BodyTemplate { get; set; }

    public string? HttpMethod { get; set; }

    // Email fields
    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? EncryptedPassword { get; set; }

    public string? FromAddress { get; set; }

    public string? ToAddresses { get; set; }

    public bool TriggerOnIpChange { get; set; }

    public bool TriggerOnFailure { get; set; }

    public bool TriggerOnSuccess { get; set; }
}
