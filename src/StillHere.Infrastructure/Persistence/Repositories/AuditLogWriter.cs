using StillHere.Application.Features.AuditLog;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogWriter(AppDbContext db) : IAuditLogWriter
{
    public async Task WriteAsync(WriteAuditLogEntryRequest request, CancellationToken cancellationToken)
    {
        db.AuditLogEntries.Add(new AuditLogEntry
        {
            ManagedDomainId = request.ManagedDomainId,
            TimestampUtc = request.TimestampUtc,
            EventType = AuditEventTypeMapper.ToEntity(request.EventType),
            OldIp = request.OldIp,
            NewIp = request.NewIp,
            Message = request.Message,
            Success = request.Success,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
