using SyntaxCircus.Common;

namespace StillHere.Application.Features.AuditLog;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLogEntryDto>> QueryAsync(
        int? managedDomainId,
        AuditEventKind? eventType,
        bool? success,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes entries older than <c>GlobalSettings.AuditLogRetentionDays</c>. A <c>null</c>
    /// retention window keeps everything and deletes nothing. Returns the number of entries removed.
    /// </summary>
    Task<int> PruneExpiredAsync(CancellationToken cancellationToken);
}
