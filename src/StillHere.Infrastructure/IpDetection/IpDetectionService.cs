using System.Net;
using System.Text.Json;
using Serilog;
using StillHere.Application.IpDetection;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace StillHere.Infrastructure.IpDetection;

internal sealed class IpDetectionService(
    IHttpClientFactory httpClientFactory,
    AppDbContext db,
    IpDetectionCache cache) : IIpDetectionService
{
    internal const string IpCheckHttpClientName = "ip-check";

    // Fewer retries than the other resilient clients: this service already iterates a fallback
    // list of external IP-check services and caches the result, so one quick attempt per service
    // beats retrying slowly through a single one.
    internal const int MaxRetryAttempts = 1;
    internal static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(5);

    public Task<IpDetectionResult> DetectCurrentIpAsync(CancellationToken cancellationToken) =>
        cache.GetOrDetectAsync(() => DetectWithoutCacheAsync(cancellationToken), cancellationToken);

    private async Task<IpDetectionResult> DetectWithoutCacheAsync(CancellationToken cancellationToken)
    {
        var settings = await db.GlobalSettings.AsNoTracking()
            .FirstAsync(s => s.Id == GlobalSettings.SingletonId, cancellationToken);

        string[] urls;
        try
        {
            urls = JsonSerializer.Deserialize<string[]>(settings.ExternalIpCheckServices) ?? [];
        }
        catch (JsonException ex)
        {
            return IpDetectionResult.Failed($"Could not parse configured IP-check service list: {ex.Message}");
        }

        var httpClient = httpClientFactory.CreateClient(IpCheckHttpClientName);

        foreach (var url in urls)
        {
            var ip = await TryDetectFromServiceAsync(httpClient, url, cancellationToken);
            if (ip is not null)
            {
                return IpDetectionResult.Succeeded(ip);
            }
        }

        Log.Warning("All {Count} configured external IP-check services failed.", urls.Length);
        return IpDetectionResult.Failed(urls.Length == 0
            ? "No external IP-check services are configured."
            : $"All {urls.Length} configured external IP-check services failed.");
    }

    private static async Task<string?> TryDetectFromServiceAsync(
        HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Log.Warning(ex, "IP-check service {Url} request failed.", url);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("IP-check service {Url} returned {StatusCode}.", url, response.StatusCode);
            return null;
        }

        var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (!IPAddress.TryParse(body, out _))
        {
            Log.Warning("IP-check service {Url} returned an unparseable body.", url);
            return null;
        }

        return body;
    }

    public ProviderIpComparisonOutcome CompareProviderReportedIp(string expectedIp, string? providerReportedIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedIp);

        if (string.IsNullOrWhiteSpace(providerReportedIp))
        {
            return ProviderIpComparisonOutcome.NotReported;
        }

        if (string.Equals(expectedIp.Trim(), providerReportedIp.Trim(), StringComparison.Ordinal))
        {
            return ProviderIpComparisonOutcome.Match;
        }

        Log.Warning(
            "Provider-reported IP {ProviderReportedIp} does not match the IP sent {ExpectedIp}.",
            providerReportedIp, expectedIp);
        return ProviderIpComparisonOutcome.Mismatch;
    }
}
