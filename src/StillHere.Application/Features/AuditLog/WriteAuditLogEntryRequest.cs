namespace StillHere.Application.Features.AuditLog;

/// <summary>
/// <paramref name="TimestampUtc"/> is caller-supplied rather than self-stamped by the writer, so a
/// single check cycle's <c>IpChanged</c> + <c>UpdateSucceeded</c>/<c>UpdateFailed</c> pair (and the
/// corresponding <c>ManagedDomain</c> row update) share exactly one timestamp instead of drifting
/// across separate <see cref="DateTime.UtcNow"/> reads.
/// </summary>
public sealed record WriteAuditLogEntryRequest(
    int? ManagedDomainId,
    AuditEventKind EventType,
    string? OldIp,
    string? NewIp,
    string Message,
    bool Success,
    DateTime TimestampUtc);
