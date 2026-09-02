using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Notifications;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class DeleteNotificationChannelRequestHandlerTests
{
    private readonly INotificationChannelRepository _notificationChannels = Substitute.For<INotificationChannelRepository>();
    private readonly DeleteNotificationChannelRequestHandler _handler;

    private static readonly NotificationChannelDto ExistingWebhook = new(
        1, NotificationChannelType.Webhook, "My Webhook", true, "https://example.com/hook", null, "POST",
        null, null, false, null, null, null, null, true, false, false);

    public DeleteNotificationChannelRequestHandlerTests()
    {
        _handler = new DeleteNotificationChannelRequestHandler(_notificationChannels);
    }

    [Fact]
    public async Task HandleAsync_ChannelNotFound_ReturnsNotFound()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((NotificationChannelDto?)null);

        var result = await _handler.HandleAsync(new DeleteNotificationChannelRequest(1), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
        result.Errors[0].Code.ShouldBe("notification-channel-not-found");
        await _notificationChannels.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChannelExists_DeletesAndReturnsSuccess()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);

        var result = await _handler.HandleAsync(new DeleteNotificationChannelRequest(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _notificationChannels.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }
}
