namespace StillHere.Application.Features.Notifications;

/// <summary>
/// A plain service-level result, not <c>SyntaxCircus.Common.Result&lt;T&gt;</c> -- this isn't a
/// named-handler outcome mapped to transport; whatever dispatches notifications across channels
/// (later in Phase 08) translates this into its own <c>Result&lt;T&gt;</c> at its own boundary.
/// </summary>
public sealed record NotificationSendResult(bool Success, string Message)
{
    public static NotificationSendResult Succeeded(string message) => new(true, message);

    public static NotificationSendResult Failed(string message) => new(false, message);
}
