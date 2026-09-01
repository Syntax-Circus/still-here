namespace StillHere.Application.Features.AuditLog;

/// <summary>
/// Deliberately only the four kinds Phase 06 writes. <c>DomainAdded</c>/<c>DomainEdited</c>/
/// <c>DomainDeleted</c>/<c>LoginSuccess</c>/<c>LoginFailure</c> exist on the underlying entity's
/// <c>AuditEventType</c> enum but stay unimplemented -- Phase 02/04 don't write any audit entries
/// today, a pre-existing gap outside this phase's scope.
/// </summary>
public enum AuditEventKind
{
    CheckOnly,
    IpChanged,
    UpdateFailed,
    UpdateSucceeded,
}
