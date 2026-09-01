using Microsoft.EntityFrameworkCore;
using StillHere.Application.Features.AuditLog;
using StillHere.Infrastructure.Persistence.Entities;
using SyntaxCircus.Common;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task<PagedResult<AuditLogEntryDto>> QueryAsync(
        int? managedDomainId,
        AuditEventKind? eventType,
        bool? success,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.AuditLogEntries.AsNoTracking().Include(e => e.ManagedDomain).AsQueryable();

        if (managedDomainId is not null)
        {
            query = query.Where(e => e.ManagedDomainId == managedDomainId);
        }

        if (eventType is not null)
        {
            var entityEventType = AuditEventTypeMapper.ToEntity(eventType.Value);
            query = query.Where(e => e.EventType == entityEventType);
        }

        if (success is not null)
        {
            query = query.Where(e => e.Success == success);
        }

        if (fromUtc is not null)
        {
            query = query.Where(e => e.TimestampUtc >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(e => e.TimestampUtc <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto>([.. entities.Select(ToDto)], page, pageSize, totalCount);
    }

    private static AuditLogEntryDto ToDto(AuditLogEntry entity) => new(
        entity.Id,
        entity.ManagedDomainId,
        entity.ManagedDomain?.DomainName,
        entity.TimestampUtc,
        AuditEventTypeMapper.ToApplication(entity.EventType),
        entity.OldIp,
        entity.NewIp,
        entity.Message,
        entity.Success);
}
