using SyntaxCircus.Common;

namespace StillHere.Application.Features.AuditLog;

public sealed record GetAuditLogEntriesRequest(
    int? ManagedDomainId,
    AuditEventKind? EventType,
    bool? Success,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);

public interface IGetAuditLogEntriesRequestHandler
{
    Task<Result<PagedResult<AuditLogEntryDto>>> HandleAsync(
        GetAuditLogEntriesRequest request, CancellationToken cancellationToken);
}

public sealed class GetAuditLogEntriesRequestHandler(
    IAuditLogRepository auditLog) : IGetAuditLogEntriesRequestHandler
{
    public async Task<Result<PagedResult<AuditLogEntryDto>>> HandleAsync(
        GetAuditLogEntriesRequest request, CancellationToken cancellationToken)
    {
        if (request.FromUtc is not null && request.ToUtc is not null && request.FromUtc > request.ToUtc)
        {
            return Result<PagedResult<AuditLogEntryDto>>.Failure(new ResultError(
                "invalid-date-range", "The start date must be before the end date.",
                ResultErrorKind.Validation, nameof(request.FromUtc)));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(
            request.PageSize <= 0 ? AuditLogPaging.DefaultPageSize : request.PageSize,
            1, AuditLogPaging.MaxPageSize);

        var result = await auditLog.QueryAsync(
            request.ManagedDomainId, request.EventType, request.Success,
            request.FromUtc, request.ToUtc, page, pageSize, cancellationToken);

        return Result<PagedResult<AuditLogEntryDto>>.Success(result);
    }
}
