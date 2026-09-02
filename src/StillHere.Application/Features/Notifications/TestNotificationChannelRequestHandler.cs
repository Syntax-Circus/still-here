using SyntaxCircus.Common;

namespace StillHere.Application.Features.Notifications;

public sealed record TestNotificationChannelRequest(int Id);

public interface ITestNotificationChannelRequestHandler
{
    Task<Result> HandleAsync(TestNotificationChannelRequest request, CancellationToken cancellationToken);
}

public sealed class TestNotificationChannelRequestHandler(
    INotificationChannelRepository notificationChannels,
    INotificationDispatcher dispatcher)
    : ITestNotificationChannelRequestHandler
{
    public async Task<Result> HandleAsync(TestNotificationChannelRequest request, CancellationToken cancellationToken)
    {
        var existing = await notificationChannels.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(new ResultError(
                "notification-channel-not-found", "Notification channel not found.", ResultErrorKind.NotFound));
        }

        var context = new NotificationEventContext(
            "test.example.com", "203.0.113.1", "203.0.113.2", "Test", "This is a test notification from still-here.");

        var result = await dispatcher.SendTestAsync(existing, context, cancellationToken);

        return result.Success
            ? Result.Success()
            : Result.Failure(new ResultError("test-send-failed", result.Message, ResultErrorKind.Failure));
    }
}
