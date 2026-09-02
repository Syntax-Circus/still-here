namespace StillHere.Application.IpDetection;

/// <summary>
/// A plain service-level result, not <c>SyntaxCircus.Common.Result&lt;T&gt;</c> -- this isn't a
/// named-handler outcome mapped to transport; the handler that calls
/// <see cref="IIpDetectionService"/> (Phase 06) translates this into its own
/// <c>Result&lt;T&gt;</c> at its own boundary.
/// </summary>
public sealed record IpDetectionResult(bool Success, string? IpAddress, string Message)
{
    public static IpDetectionResult Succeeded(string ipAddress) =>
        new(true, ipAddress, $"Detected external IP {ipAddress}.");

    public static IpDetectionResult Failed(string message) =>
        new(false, null, message);
}
