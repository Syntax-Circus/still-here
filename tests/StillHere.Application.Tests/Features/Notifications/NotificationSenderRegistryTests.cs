using Shouldly;
using StillHere.Application.Features.Notifications;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class NotificationSenderRegistryTests
{
    [Fact]
    public void GetByType_KnownType_ReturnsMatchingSender()
    {
        var webhook = new FakeNotificationSender(NotificationChannelType.Webhook);
        var email = new FakeNotificationSender(NotificationChannelType.Email);
        var registry = new NotificationSenderRegistry([webhook, email]);

        registry.GetByType(NotificationChannelType.Email).ShouldBeSameAs(email);
    }

    [Fact]
    public void GetByType_UnknownType_Throws()
    {
        var registry = new NotificationSenderRegistry([new FakeNotificationSender(NotificationChannelType.Webhook)]);

        Should.Throw<InvalidOperationException>(() => registry.GetByType(NotificationChannelType.Email));
    }

    [Fact]
    public void Senders_ListsEveryRegisteredSender()
    {
        var webhook = new FakeNotificationSender(NotificationChannelType.Webhook);
        var email = new FakeNotificationSender(NotificationChannelType.Email);
        var registry = new NotificationSenderRegistry([webhook, email]);

        registry.Senders.ShouldBe([webhook, email]);
    }

    private sealed class FakeNotificationSender(NotificationChannelType channelType) : INotificationSender
    {
        public NotificationChannelType ChannelType => channelType;

        public Task<NotificationSendResult> SendAsync(
            NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not needed for registry tests.");
    }
}
