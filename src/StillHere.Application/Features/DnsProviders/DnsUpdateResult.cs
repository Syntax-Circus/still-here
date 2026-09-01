namespace StillHere.Application.Features.DnsProviders;

/// <summary>
/// A plain service-level result, not <c>SyntaxCircus.Common.Result&lt;T&gt;</c> -- this isn't a
/// named-handler outcome mapped to transport; the handlers that call <see cref="IDnsProvider"/>
/// (Phase 06) translate this into their own <c>Result&lt;T&gt;</c> at their own boundary.
/// </summary>
public sealed record DnsUpdateResult(bool Success, string? ProviderReportedIp, string Message)
{
    public static DnsUpdateResult Succeeded(string? providerReportedIp, string message) =>
        new(true, providerReportedIp, message);

    public static DnsUpdateResult Failed(string message) =>
        new(false, null, message);
}
