using StillHere.Application.Features.Domains;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.DomainChecks;

public sealed record ManualDomainCheckRequest(int ManagedDomainId);

public interface IRunManualDomainCheckRequestHandler
{
    Task<Result<DomainCheckOutcomeDto>> HandleAsync(ManualDomainCheckRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Thin reuse of <see cref="IRunScheduledDomainCheckHandler"/> -- per PHASE-06-scheduler.md's
/// Architecture Decision, "the same handler logic" without a third executor abstraction. No
/// <c>Enabled</c> check: FR-8 bypasses the schedule, not the enabled flag -- whether a disabled
/// domain's "check now" button even renders is a Phase 07 UI decision.
/// </summary>
public sealed class RunManualDomainCheckRequestHandler(
    IManagedDomainRepository managedDomains,
    IRunScheduledDomainCheckHandler scheduledCheck) : IRunManualDomainCheckRequestHandler
{
    public async Task<Result<DomainCheckOutcomeDto>> HandleAsync(
        ManualDomainCheckRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await managedDomains.FindByIdAsync(request.ManagedDomainId, cancellationToken);
        if (existing is null)
        {
            return Result<DomainCheckOutcomeDto>.Failure(new ResultError(
                "domain-not-found", "Domain not found.", ResultErrorKind.NotFound));
        }

        var outcome = await scheduledCheck.HandleAsync(request.ManagedDomainId, cancellationToken);
        return Result<DomainCheckOutcomeDto>.Success(outcome);
    }
}
