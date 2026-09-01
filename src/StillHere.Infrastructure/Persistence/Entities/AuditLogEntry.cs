namespace StillHere.Infrastructure.Persistence.Entities;

internal sealed class AuditLogEntry
{
    public int Id { get; set; }

    public int? ManagedDomainId { get; set; }

    public ManagedDomain? ManagedDomain { get; set; }

    public DateTime TimestampUtc { get; set; }

    public AuditEventType EventType { get; set; }

    public string? OldIp { get; set; }

    public string? NewIp { get; set; }

    public required string Message { get; set; }

    public bool Success { get; set; }
}
