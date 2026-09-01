namespace StillHere.Application.IpDetection;

public interface IIpDetectionService
{
    Task<IpDetectionResult> DetectCurrentIpAsync(CancellationToken cancellationToken);
}
