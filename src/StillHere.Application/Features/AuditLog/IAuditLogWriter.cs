namespace StillHere.Application.Features.AuditLog;

public interface IAuditLogWriter
{
    Task WriteAsync(WriteAuditLogEntryRequest request, CancellationToken cancellationToken);
}
