using System.Text;
using System.Text.Json;
using StillHere.Application.Features.Notifications;

namespace StillHere.Infrastructure.Notifications;

/// <summary>
/// Resilience/retry is the resilient <see cref="HttpClient"/>'s job (see
/// <c>DependencyInjection.AddInfrastructure</c>'s "notification-webhook" registration), not this
/// class's -- this class only ever catches an exception to translate it into a
/// <see cref="NotificationSendResult"/>, matching <c>NamecheapDnsProvider</c>'s division of
/// concerns: never let an exception escape <see cref="SendAsync"/>.
/// </summary>
internal sealed class WebhookNotificationSender(HttpClient httpClient) : INotificationSender
{
    // A notification send is best-effort, not costly to lose -- fewer retries than the DDNS
    // update client, but more than the IP-check client since a webhook target has no fallback list.
    internal const int MaxRetryAttempts = 2;
    internal static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

    public NotificationChannelType ChannelType => NotificationChannelType.Webhook;

    public async Task<NotificationSendResult> SendAsync(
        NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            // channel.BodyTemplate is null when the admin hasn't authored a custom template, so the
            // sender's own default applies. That default is serialized straight from the event
            // context -- never through NotificationTemplateSubstitutor's bare string.Replace chain --
            // because context.Message can carry untrusted-ish third-party text (e.g. a provider's raw
            // error XML) containing quotes/backslashes/newlines that would otherwise produce
            // malformed JSON. An admin-authored custom BodyTemplate is still run through the
            // substitutor: the admin controls that template (and its escaping) and may be targeting a
            // non-JSON payload shape entirely.
            var body = channel.BodyTemplate is null
                ? JsonSerializer.Serialize(new
                {
                    domain = context.DomainName,
                    oldIp = context.OldIp ?? "",
                    newIp = context.NewIp ?? "",
                    status = context.Status,
                    message = context.Message,
                })
                : NotificationTemplateSubstitutor.Substitute(channel.BodyTemplate, context);

            var httpMethod = string.IsNullOrWhiteSpace(channel.HttpMethod) ? "POST" : channel.HttpMethod;

            var request = new HttpRequestMessage(new HttpMethod(httpMethod), channel.Url!)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? NotificationSendResult.Succeeded("Webhook notification sent.")
                : NotificationSendResult.Failed($"Webhook returned status code {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return NotificationSendResult.Failed($"Webhook request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return NotificationSendResult.Failed($"Webhook request timed out: {ex.Message}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UriFormatException or ArgumentException or FormatException)
        {
            // Malformed/missing channel.Url fails URI resolution (no BaseAddress is configured on
            // this HttpClient); malformed channel.HttpMethod (e.g. a trailing space or embedded
            // whitespace) fails `new HttpMethod(...)` construction with a plain FormatException --
            // note UriFormatException derives from FormatException but catching the derived type
            // does not also catch the base type, so both must be listed explicitly. Matches
            // NamecheapDnsProvider's catch-all-at-the-boundary convention: never let an exception
            // escape SendAsync.
            return NotificationSendResult.Failed($"Invalid webhook configuration: {ex.Message}");
        }
    }
}
