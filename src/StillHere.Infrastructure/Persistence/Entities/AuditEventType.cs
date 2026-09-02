namespace StillHere.Infrastructure.Persistence.Entities;

internal enum AuditEventType
{
    CheckOnly,
    IpChanged,
    UpdateFailed,
    UpdateSucceeded,
    DomainAdded,
    DomainEdited,
    DomainDeleted,
    LoginSuccess,
    LoginFailure,
}
