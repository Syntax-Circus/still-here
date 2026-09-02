namespace StillHere.Application.Features.AuditLog;

public sealed record AuditLogEntryDto(
    int Id,
    int? ManagedDomainId,
    string? ManagedDomainName,
    DateTime TimestampUtc,
    AuditEventKind EventType,
    string? OldIp,
    string? NewIp,
    string Message,
    bool Success);
