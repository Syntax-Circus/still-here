namespace StillHere.Application.Features.Notifications;

public interface INotificationSenderRegistry
{
    IReadOnlyList<INotificationSender> Senders { get; }

    INotificationSender GetByType(NotificationChannelType type);
}
