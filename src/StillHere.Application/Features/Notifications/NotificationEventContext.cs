namespace StillHere.Application.Features.Notifications;

public sealed record NotificationEventContext(string DomainName, string? OldIp, string? NewIp, string Status, string Message);
