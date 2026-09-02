using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StillHere.Application.Features.AuditLog;
using SyntaxCircus.Common;

namespace StillHere.Infrastructure.Scheduling;

internal sealed partial class AuditLogRetentionScheduler(
    IServiceScopeFactory scopeFactory,
    TimeSpan tickInterval,
    ILogger<AuditLogRetentionScheduler> logger) : PeriodicBackgroundService(tickInterval, logger)
{
    public const int DefaultTickIntervalSeconds = 86_400; // once a day -- retention pruning doesn't need domain-check frequency

    // C# doesn't allow widening an override's accessibility, so the real logic lives in this
    // internal method (callable directly by StillHere.Infrastructure.Tests, already
    // InternalsVisibleTo) and the protected override just forwards to it.
    protected override Task ExecuteTickAsync(CancellationToken cancellationToken) => RunTickAsync(cancellationToken);

    internal async Task RunTickAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var deletedCount = await scope.ServiceProvider.GetRequiredService<IPruneExpiredAuditLogEntriesHandler>()
            .HandleAsync(cancellationToken);

        LogPruned(logger, deletedCount);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pruned {DeletedCount} expired audit log entries.")]
    private static partial void LogPruned(ILogger logger, int deletedCount);
}
