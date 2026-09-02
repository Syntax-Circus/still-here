using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Notifications;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class TestNotificationChannelRequestHandlerTests
{
    private readonly INotificationChannelRepository _notificationChannels = Substitute.For<INotificationChannelRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TestNotificationChannelRequestHandler _handler;

    private static readonly NotificationChannelDto ExistingWebhook = new(
        1, NotificationChannelType.Webhook, "My Webhook", true, "https://example.com/hook", null, "POST",
        null, null, false, null, null, null, null, true, false, false);

    public TestNotificationChannelRequestHandlerTests()
    {
        _handler = new TestNotificationChannelRequestHandler(_notificationChannels, _dispatcher);
    }

    [Fact]
    public async Task HandleAsync_ChannelNotFound_ReturnsNotFound()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((NotificationChannelDto?)null);

        var result = await _handler.HandleAsync(new TestNotificationChannelRequest(1), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
        result.Errors[0].Code.ShouldBe("notification-channel-not-found");
        await _dispatcher.DidNotReceive().SendTestAsync(
            Arg.Any<NotificationChannelDto>(), Arg.Any<NotificationEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DispatcherSucceeds_ReturnsSuccess()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);
        _dispatcher.SendTestAsync(Arg.Any<NotificationChannelDto>(), Arg.Any<NotificationEventContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Succeeded("Sent."));

        var result = await _handler.HandleAsync(new TestNotificationChannelRequest(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _dispatcher.Received(1).SendTestAsync(
            ExistingWebhook,
            Arg.Is<NotificationEventContext>(c => c.DomainName == "test.example.com" && c.Status == "Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DispatcherFails_ReturnsFailureWithMessage()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);
        _dispatcher.SendTestAsync(Arg.Any<NotificationChannelDto>(), Arg.Any<NotificationEventContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Failed("Connection refused."));

        var result = await _handler.HandleAsync(new TestNotificationChannelRequest(1), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Failure);
        result.Errors[0].Code.ShouldBe("test-send-failed");
        result.Errors[0].Message.ShouldBe("Connection refused.");
    }
}
