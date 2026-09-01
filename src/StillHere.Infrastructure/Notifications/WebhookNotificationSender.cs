using System.Text;
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
    private const string DefaultBodyTemplate =
        """{"domain":"{domain}","oldIp":"{oldIp}","newIp":"{newIp}","status":"{status}","message":"{message}"}""";

    public NotificationChannelType ChannelType => NotificationChannelType.Webhook;

    public async Task<NotificationSendResult> SendAsync(
        NotificationChannelDto channel, NotificationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var body = NotificationTemplateSubstitutor.Substitute(channel.BodyTemplate ?? DefaultBodyTemplate, context);

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
        catch (Exception ex) when (ex is InvalidOperationException or UriFormatException or ArgumentException)
        {
            // Malformed/missing channel.Url or channel.HttpMethod fails request construction or URI
            // resolution (no BaseAddress is configured on this HttpClient) -- matches
            // NamecheapDnsProvider's catch-all-at-the-boundary convention: never let an exception
            // escape SendAsync.
            return NotificationSendResult.Failed($"Invalid webhook configuration: {ex.Message}");
        }
    }
}
