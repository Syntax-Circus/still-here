namespace StillHere.Application.Features.Notifications;

public interface INotificationSender
{
    NotificationChannelType ChannelType { get; }

    Task<NotificationSendResult> SendAsync(
        NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken);
}
