using System.Text;
using System.Xml;
using System.Xml.Linq;
using StillHere.Application.Features.DnsProviders;

namespace StillHere.Infrastructure.DnsProviders;

/// <summary>
/// Namecheap's DDNS update API always returns HTTP 200, even on API-level errors -- success or
/// failure must be read from the XML body (ErrCount/Done), never the transport status code. The
/// response also always declares "utf-16" encoding while the bytes are actually UTF-8 (a known
/// Namecheap bug); decoding explicitly as UTF-8 and parsing the resulting string (rather than the
/// byte stream) sidesteps the false declaration entirely.
/// </summary>
internal sealed class NamecheapDnsProvider(HttpClient httpClient) : IDnsProvider
{
    private const string PasswordFieldKey = "Password";

    public string ProviderKey => "namecheap";

    public string DisplayName => "Namecheap";

    public IReadOnlyList<ProviderCredentialField> CredentialFields { get; } =
        [new ProviderCredentialField(PasswordFieldKey, "Dynamic DNS Password", IsSecret: true)];

    public async Task<DnsUpdateResult> UpdateAsync(DnsUpdateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.CredentialSecrets.TryGetValue(PasswordFieldKey, out var password) || string.IsNullOrWhiteSpace(password))
        {
            return DnsUpdateResult.Failed("Missing Namecheap Dynamic DNS password.");
        }

        var query = $"update?host={Uri.EscapeDataString(request.Host)}" +
            $"&domain={Uri.EscapeDataString(request.DomainName)}" +
            $"&password={Uri.EscapeDataString(password)}" +
            $"&ip={Uri.EscapeDataString(request.NewIp)}";

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(query, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return DnsUpdateResult.Failed($"Namecheap request failed: {ex.Message}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var xml = Encoding.UTF8.GetString(bytes);

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            return DnsUpdateResult.Failed($"Could not parse Namecheap response: {ex.Message}");
        }

        var root = document.Root;
        var errCountParsed = int.TryParse(root?.Element("ErrCount")?.Value, out var errCount);
        var done = string.Equals(root?.Element("Done")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var reportedIp = root?.Element("IP")?.Value;

        if (!errCountParsed || errCount != 0 || !done)
        {
            var errorMessages = root?.Element("errors")?.Elements().Select(e => e.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList() ?? [];

            var message = errorMessages.Count > 0
                ? string.Join("; ", errorMessages)
                : "Namecheap reported an unspecified error.";

            return DnsUpdateResult.Failed(message);
        }

        return DnsUpdateResult.Succeeded(reportedIp, "Namecheap DDNS update succeeded.");
    }
}
