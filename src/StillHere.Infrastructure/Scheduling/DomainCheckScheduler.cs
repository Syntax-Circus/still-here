using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StillHere.Application.Features.DomainChecks;
using StillHere.Infrastructure.Persistence;
using SyntaxCircus.Common;

namespace StillHere.Infrastructure.Scheduling;

internal sealed partial class DomainCheckScheduler(
    IServiceScopeFactory scopeFactory,
    TimeSpan tickInterval,
    ILogger<DomainCheckScheduler> logger) : PeriodicBackgroundService(tickInterval, logger)
{
    public const int DefaultTickIntervalSeconds = 30; // FR-14's "~30s base interval"

    // C# doesn't allow widening an override's accessibility, so the real logic lives in this
    // internal method (callable directly by StillHere.Infrastructure.Tests, already
    // InternalsVisibleTo) and the protected override just forwards to it.
    protected override Task ExecuteTickAsync(CancellationToken cancellationToken) => RunTickAsync(cancellationToken);

    internal async Task RunTickAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        // WebApplicationFactory-based tests never reach Program.cs's own post-Build() migration
        // line, so the scheduler migrates itself defensively -- a cheap no-op once real migrations
        // have already run (see AuthEndpointsTests.InitializeAsync() for the same root cause).
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);

        var dueIds = await scope.ServiceProvider.GetRequiredService<IListDueDomainsHandler>()
            .HandleAsync(DateTime.UtcNow, cancellationToken);
        var checkHandler = scope.ServiceProvider.GetRequiredService<IRunScheduledDomainCheckHandler>();

        // One scope is reused for every due domain in this tick (not one scope per domain) --
        // a single scheduler instance, ~12 domains, negligible change-tracker growth; per-domain
        // failure isolation is already achieved by the try/catch below, not by scope isolation.
        foreach (var id in dueIds)
        {
            try
            {
                await checkHandler.HandleAsync(id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogScheduledCheckFailed(logger, ex, id);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduled check failed for ManagedDomainId {ManagedDomainId}.")]
    private static partial void LogScheduledCheckFailed(ILogger logger, Exception ex, int managedDomainId);
}
