namespace StillHere.Application.Features.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationTrigger trigger, NotificationEventContext context, CancellationToken cancellationToken);

    Task<NotificationSendResult> SendTestAsync(NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken);
}
