using StillHere.Application.Features.AuditLog;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal static class AuditEventTypeMapper
{
    public static AuditEventType ToEntity(AuditEventKind kind) => kind switch
    {
        AuditEventKind.CheckOnly => AuditEventType.CheckOnly,
        AuditEventKind.IpChanged => AuditEventType.IpChanged,
        AuditEventKind.UpdateFailed => AuditEventType.UpdateFailed,
        AuditEventKind.UpdateSucceeded => AuditEventType.UpdateSucceeded,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static AuditEventKind ToApplication(AuditEventType type) => type switch
    {
        AuditEventType.CheckOnly => AuditEventKind.CheckOnly,
        AuditEventType.IpChanged => AuditEventKind.IpChanged,
        AuditEventType.UpdateFailed => AuditEventKind.UpdateFailed,
        AuditEventType.UpdateSucceeded => AuditEventKind.UpdateSucceeded,
        // DomainAdded/DomainEdited/DomainDeleted/LoginSuccess/LoginFailure exist on the entity enum
        // but nothing writes them yet (same gap noted on AuditEventKind) -- throwing here is
        // defensive, matching the write-side's existing throw-on-unmapped-value behavior.
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
