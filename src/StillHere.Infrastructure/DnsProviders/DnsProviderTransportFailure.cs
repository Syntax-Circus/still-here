using Polly.CircuitBreaker;
using Polly.Timeout;
using StillHere.Application.Features.DnsProviders;

namespace StillHere.Infrastructure.DnsProviders;

/// <summary>
/// Maps transport-level exceptions (network errors, timeouts, an open circuit breaker from the
/// resilient HTTP pipeline) to a failed <see cref="DnsUpdateResult"/> so a provider never lets them
/// escape as unhandled errors -- the scheduler and audit log expect a recorded failed update.
/// Caller-requested cancellation is deliberately not matched and still propagates.
/// </summary>
internal static class DnsProviderTransportFailure
{
    public static bool TryCreate(
        Exception exception, string providerName, CancellationToken cancellationToken, out DnsUpdateResult failure)
    {
        switch (exception)
        {
            case HttpRequestException http:
                failure = DnsUpdateResult.Failed($"{providerName} request failed: {http.Message}");
                return true;
            case BrokenCircuitException:
                failure = DnsUpdateResult.Failed(
                    $"{providerName} is temporarily unavailable (circuit open after repeated failures).");
                return true;
            case TimeoutRejectedException:
            case OperationCanceledException when !cancellationToken.IsCancellationRequested:
                // HttpClient.Timeout surfaces as a cancellation the caller never requested.
                failure = DnsUpdateResult.Failed($"{providerName} request timed out.");
                return true;
            default:
                failure = null!;
                return false;
        }
    }
}
