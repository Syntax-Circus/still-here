namespace StillHere.Application.IpDetection;

public enum ProviderIpComparisonOutcome
{
    /// <summary>The provider's update response didn't include a reported IP (null or blank).</summary>
    NotReported,
    Match,
    Mismatch,
}
