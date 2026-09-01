namespace StillHere.Application.IpDetection;

public interface IIpDetectionService
{
    Task<IpDetectionResult> DetectCurrentIpAsync(CancellationToken cancellationToken);

    ProviderIpComparisonOutcome CompareProviderReportedIp(string expectedIp, string? providerReportedIp);
}
