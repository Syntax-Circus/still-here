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
}
