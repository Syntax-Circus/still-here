using StillHere.Application.Features.Domains;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Dashboard;

public interface IGetDashboardSummaryRequestHandler
{
    Task<Result<DashboardSummaryDto>> HandleAsync(CancellationToken cancellationToken);
}

public sealed class GetDashboardSummaryRequestHandler(
    IManagedDomainRepository managedDomains) : IGetDashboardSummaryRequestHandler
{
    public async Task<Result<DashboardSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var domains = await managedDomains.ListDashboardSummariesAsync(cancellationToken);
        return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto(domains));
    }
}
