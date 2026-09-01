using StillHere.Application.Features.Domains;

namespace StillHere.Application.Features.Dashboard;

public sealed record DashboardSummaryDto(IReadOnlyList<ManagedDomainSummaryDto> Domains);
