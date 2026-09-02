namespace StillHere.Application.Features.AuditLog;

public interface IPruneExpiredAuditLogEntriesHandler
{
    Task<int> HandleAsync(CancellationToken cancellationToken);
}

public sealed class PruneExpiredAuditLogEntriesHandler(IAuditLogRepository auditLog)
    : IPruneExpiredAuditLogEntriesHandler
{
    public Task<int> HandleAsync(CancellationToken cancellationToken) =>
        auditLog.PruneExpiredAsync(cancellationToken);
}
