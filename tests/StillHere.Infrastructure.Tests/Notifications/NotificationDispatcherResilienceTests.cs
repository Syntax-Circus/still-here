using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Infrastructure.Notifications;
using Xunit;

namespace StillHere.Infrastructure.Tests.Notifications;

/// <summary>
/// Notifications are best-effort and must never affect the check flow. These tests cover failure
/// modes above and beyond <see cref="NotificationDispatcherAuditIsolationTests"/>'s per-channel-send
/// failure case: a failure in listing channels itself (<see cref="DispatchAsync"/>), and a sender
/// that throws during a test-send (<see cref="SendTestAsync"/>), which -- unlike the fire-and-forget
/// <c>DispatchAsync</c> loop -- surfaces its result directly to the UI and so must convert any
/// exception into a failed result rather than ever propagating it.
/// </summary>
public sealed class NotificationDispatcherResilienceTests
{
    private static readonly NotificationChannelDto Channel = new(
        1, NotificationChannelType.Webhook, "My Webhook", true, "https://example.com/hook", null, "POST",
        null, null, false, null, null, null, null, true, true, true);

    private static readonly NotificationEventContext Context =
        new("example.com", "1.1.1.1", "2.2.2.2", "IpChanged", "IP changed.");

    [Fact]
    public async Task DispatchAsync_ListEnabledAsyncThrows_DoesNotPropagate()
    {
        var channels = Substitute.For<INotificationChannelRepository>();
        channels.ListEnabledAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Simulated database failure."));
        var senders = Substitute.For<INotificationSenderRegistry>();

        var dispatcher = new NotificationDispatcher(channels, senders, NullLogger<NotificationDispatcher>.Instance);

        await Should.NotThrowAsync(() =>
            dispatcher.DispatchAsync(NotificationTrigger.IpChange, Context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendTestAsync_SenderThrows_ReturnsFailedResultWithoutPropagating()
    {
        var channels = Substitute.For<INotificationChannelRepository>();
        var failingSender = Substitute.For<INotificationSender>();
        failingSender.ChannelType.Returns(NotificationChannelType.Webhook);
        failingSender
            .SendAsync(Arg.Any<NotificationChannelDto>(), Arg.Any<NotificationEventContext>(), Arg.Any<CancellationToken>())
            .Throws(new FormatException("Simulated malformed HTTP method."));
        var senders = Substitute.For<INotificationSenderRegistry>();
        senders.GetByType(NotificationChannelType.Webhook).Returns(failingSender);

        var dispatcher = new NotificationDispatcher(channels, senders, NullLogger<NotificationDispatcher>.Instance);

        var result = await dispatcher.SendTestAsync(Channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }
}
