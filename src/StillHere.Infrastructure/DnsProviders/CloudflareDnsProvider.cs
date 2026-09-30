using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StillHere.Application.Features.DnsProviders;

namespace StillHere.Infrastructure.DnsProviders;

/// <summary>
/// Updates existing A records through the Cloudflare v4 API using a scoped API token
/// (Zone &gt; DNS &gt; Edit). The zone id is resolved from the domain name and the record from its
/// FQDN. Records are never created -- a missing zone or record is reported as a failure, matching
/// Namecheap's update-only behavior. Ambiguity is also a failure rather than a guess: more than one
/// matching zone or more than one A record for the host is refused, so an update is never partial. Every Cloudflare response carries a <c>success</c> flag and an
/// <c>errors</c> array, which are read in preference to the transport status code.
/// </summary>
internal sealed class CloudflareDnsProvider(HttpClient httpClient) : IDnsProvider
{
    private const string ApiTokenFieldKey = "ApiToken";

    internal const int MaxRetryAttempts = 3;
    internal static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(15);

    public string ProviderKey => "cloudflare";

    public string DisplayName => "Cloudflare";

    public IReadOnlyList<ProviderCredentialField> CredentialFields { get; } =
        [new ProviderCredentialField(ApiTokenFieldKey, "API Token (Zone > DNS > Edit)", IsSecret: true)];

    public async Task<DnsUpdateResult> UpdateAsync(DnsUpdateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.CredentialSecrets.TryGetValue(ApiTokenFieldKey, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return DnsUpdateResult.Failed("Missing Cloudflare API token.");
        }

        var fqdn = request.Host == "@" ? request.DomainName : $"{request.Host}.{request.DomainName}";

        try
        {
            var zones = await SendAsync(
                HttpMethod.Get, $"zones?name={Uri.EscapeDataString(request.DomainName)}", token, body: null, cancellationToken);
            if (zones.Error is not null)
            {
                return DnsUpdateResult.Failed(zones.Error);
            }

            if (zones.Results.Count > 1)
            {
                // e.g. a token spanning several accounts that each hold a zone of this name. Guessing
                // could update the wrong zone, so require the token be scoped to a single one.
                return DnsUpdateResult.Failed($"Cloudflare returned multiple zones named {request.DomainName}. Scope the API token to a single zone.");
            }

            var zoneId = FirstString(zones.Results, "id");
            if (zoneId is null)
            {
                return DnsUpdateResult.Failed($"Cloudflare zone not found for {request.DomainName}. Check the token has access to this zone.");
            }

            var records = await SendAsync(
                HttpMethod.Get,
                $"zones/{zoneId}/dns_records?type=A&name={Uri.EscapeDataString(fqdn)}",
                token,
                body: null,
                cancellationToken);
            if (records.Error is not null)
            {
                return DnsUpdateResult.Failed(records.Error);
            }

            if (records.Results.Count == 0)
            {
                return DnsUpdateResult.Failed($"Cloudflare A record not found for {fqdn}. Create the record in Cloudflare first.");
            }

            if (records.Results.Count > 1)
            {
                // Updating some of several A records (e.g. round-robin) risks a partial update and
                // overwriting records the user meant to keep, so refuse rather than guess.
                return DnsUpdateResult.Failed($"Cloudflare has {records.Results.Count} A records for {fqdn}. Leave exactly one so still-here knows which to update.");
            }

            var record = records.Results[0];
            if (record.TryGetProperty("content", out var content) && content.GetString() == request.NewIp)
            {
                return DnsUpdateResult.Succeeded(request.NewIp, "Cloudflare DNS record already up to date.");
            }

            var recordId = record.GetProperty("id").GetString();
            var patch = await SendAsync(
                HttpMethod.Patch,
                $"zones/{zoneId}/dns_records/{recordId}",
                token,
                JsonContent.Create(new { content = request.NewIp }),
                cancellationToken);
            if (patch.Error is not null)
            {
                return DnsUpdateResult.Failed(patch.Error);
            }

            return DnsUpdateResult.Succeeded(request.NewIp, "Cloudflare DNS update succeeded.");
        }
        catch (Exception ex) when (DnsProviderTransportFailure.TryCreate(ex, "Cloudflare", cancellationToken, out var failure))
        {
            return failure;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return DnsUpdateResult.Failed($"Could not parse Cloudflare response: {ex.Message}");
        }
    }

    private async Task<ApiResponse> SendAsync(
        HttpMethod method, string relativeUrl, string token, HttpContent? body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, relativeUrl) { Content = body };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            var errorMessage = root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array
                ? errors.EnumerateArray()
                    .Select(e => e.TryGetProperty("message", out var m) ? m.GetString() : null)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                : null;

            return new ApiResponse([], errorMessage is null
                ? $"Cloudflare reported an unspecified error (HTTP {(int)response.StatusCode})."
                : $"Cloudflare error: {errorMessage}");
        }

        // Clone so the elements outlive the JsonDocument disposed at the end of this method.
        IReadOnlyList<JsonElement> results = root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array
            ? result.EnumerateArray().Select(e => e.Clone()).ToList()
            : [];

        return new ApiResponse(results, Error: null);
    }

    private static string? FirstString(IReadOnlyList<JsonElement> results, string property) =>
        results.Count > 0 && results[0].TryGetProperty(property, out var value) ? value.GetString() : null;

    private sealed record ApiResponse(IReadOnlyList<JsonElement> Results, string? Error);
}
