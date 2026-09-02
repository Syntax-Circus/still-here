using Microsoft.Extensions.Logging;
using StillHere.Application.Features.Notifications;

namespace StillHere.Infrastructure.Notifications;

/// <summary>
/// FR-22: notification send failures go to the app log only, never the audit log. This class has
/// no dependency on <c>IAuditLogWriter</c> or any audit-log abstraction whatsoever -- it is
/// structurally incapable of writing to the audit log because it never holds a reference to
/// anything that could. Do not add one, even for logging consistency with
/// <c>RunScheduledDomainCheckHandler</c>.
/// </summary>
internal sealed partial class NotificationDispatcher(
    INotificationChannelRepository channels,
    INotificationSenderRegistry senders,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task DispatchAsync(NotificationTrigger trigger, NotificationEventContext context, CancellationToken cancellationToken)
    {
        try
        {
            var enabled = await channels.ListEnabledAsync(cancellationToken);

            var matching = enabled.Where(channel => TriggerMatches(channel, trigger));

            foreach (var channel in matching)
            {
                try
                {
                    var result = await senders.GetByType(channel.Type).SendAsync(channel, context, cancellationToken);

                    if (!result.Success)
                    {
                        LogNotificationSendFailed(logger, channel.Id, channel.Name, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    LogNotificationSendThrew(logger, ex, channel.Id, channel.Name);
                }
            }
        }
        catch (Exception ex)
        {
            // Notifications are best-effort and must never affect the check flow -- even a failure
            // to list channels (e.g. a transient SQLite error) must not propagate out of here, since
            // callers such as RunScheduledDomainCheckHandler have no try/catch of their own around
            // DispatchAsync.
            LogNotificationDispatchThrew(logger, ex, trigger);
        }
    }

    public async Task<NotificationSendResult> SendTestAsync(NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(context);

        INotificationSender sender;
        try
        {
            sender = senders.GetByType(channel.Type);
        }
        catch (InvalidOperationException ex)
        {
            // No sender registered for channel.Type -- test-send must never crash the UI action.
            return NotificationSendResult.Failed($"No notification sender available for channel type '{channel.Type}': {ex.Message}");
        }

        try
        {
            return await sender.SendAsync(channel, context, cancellationToken);
        }
        catch (Exception ex)
        {
            // A sender's own catch-all-at-the-boundary convention should already prevent this, but
            // SendTestAsync must never let an exception escape under any circumstance -- unlike
            // DispatchAsync's fire-and-forget loop, this result is surfaced directly to the UI.
            return NotificationSendResult.Failed($"Test-send failed: {ex.Message}");
        }
    }

    private static bool TriggerMatches(NotificationChannelDto channel, NotificationTrigger trigger) => trigger switch
    {
        NotificationTrigger.IpChange => channel.TriggerOnIpChange,
        NotificationTrigger.Success => channel.TriggerOnSuccess,
        NotificationTrigger.Failure => channel.TriggerOnFailure,
        _ => false,
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification send failed for channel {ChannelId} ({ChannelName}): {Message}")]
    private static partial void LogNotificationSendFailed(ILogger logger, int channelId, string channelName, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification send threw for channel {ChannelId} ({ChannelName}).")]
    private static partial void LogNotificationSendThrew(ILogger logger, Exception ex, int channelId, string channelName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification dispatch threw while listing channels for trigger {Trigger}.")]
    private static partial void LogNotificationDispatchThrew(ILogger logger, Exception ex, NotificationTrigger trigger);
}
