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

        return await sender.SendAsync(channel, context, cancellationToken);
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
}
