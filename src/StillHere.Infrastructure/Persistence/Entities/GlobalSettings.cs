namespace StillHere.Infrastructure.Persistence.Entities;

/// <summary>Single-row settings table. The row with <see cref="Id"/> == <see cref="SingletonId"/> is the only one that should ever exist.</summary>
internal sealed class GlobalSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int DefaultPollingIntervalSeconds { get; set; } = 300;

    public required string IpDetectionMode { get; set; }

    /// <summary>Ordered JSON array of fallback external IP-check service URLs.</summary>
    public required string ExternalIpCheckServices { get; set; }

    public int? AuditLogRetentionDays { get; set; }
}
