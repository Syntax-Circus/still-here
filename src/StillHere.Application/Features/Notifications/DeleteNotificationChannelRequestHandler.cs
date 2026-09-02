using SyntaxCircus.Common;

namespace StillHere.Application.Features.Notifications;

public sealed record DeleteNotificationChannelRequest(int Id);

public interface IDeleteNotificationChannelRequestHandler
{
    Task<Result> HandleAsync(DeleteNotificationChannelRequest request, CancellationToken cancellationToken);
}

public sealed class DeleteNotificationChannelRequestHandler(INotificationChannelRepository notificationChannels)
    : IDeleteNotificationChannelRequestHandler
{
    public async Task<Result> HandleAsync(DeleteNotificationChannelRequest request, CancellationToken cancellationToken)
    {
        var existing = await notificationChannels.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(new ResultError(
                "notification-channel-not-found", "Notification channel not found.", ResultErrorKind.NotFound));
        }

        await notificationChannels.DeleteAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}
