namespace StillHere.Application.Features.Notifications;

public static class NotificationTemplateSubstitutor
{
    public static string Substitute(string template, NotificationEventContext context)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        return template
            .Replace("{domain}", context.DomainName)
            .Replace("{oldIp}", context.OldIp ?? "")
            .Replace("{newIp}", context.NewIp ?? "")
            .Replace("{status}", context.Status)
            .Replace("{message}", context.Message);
    }
}
