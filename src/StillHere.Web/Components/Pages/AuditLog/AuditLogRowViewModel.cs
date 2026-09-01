using StillHere.Application.Features.AuditLog;

namespace StillHere.Web.Components.Pages.AuditLog;

internal sealed record AuditLogRowViewModel(
    int Id,
    int? ManagedDomainId,
    string? DomainName,
    DateTime TimestampUtc,
    AuditEventKind EventType,
    string IpDiffDisplay,
    string Message,
    bool Success);
