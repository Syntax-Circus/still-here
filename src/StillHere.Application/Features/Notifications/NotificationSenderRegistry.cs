namespace StillHere.Application.Features.Notifications;

public sealed class NotificationSenderRegistry : INotificationSenderRegistry
{
    private readonly IReadOnlyList<INotificationSender> _senders;

    public NotificationSenderRegistry(IEnumerable<INotificationSender> senders)
    {
        ArgumentNullException.ThrowIfNull(senders);
        _senders = [.. senders];
    }

    public IReadOnlyList<INotificationSender> Senders => _senders;

    public INotificationSender GetByType(NotificationChannelType type)
    {
        foreach (var sender in _senders)
        {
            if (sender.ChannelType == type)
            {
                return sender;
            }
        }

        throw new InvalidOperationException($"No notification sender registered for channel type '{type}'.");
    }
}
