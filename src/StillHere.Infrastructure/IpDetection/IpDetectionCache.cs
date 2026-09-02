using StillHere.Application.IpDetection;

namespace StillHere.Infrastructure.IpDetection;

/// <summary>
/// Shares one successful external IP lookup across every caller within the cache window --
/// so a scheduler tick's due domains, and a "check now" click that lands shortly after, don't
/// each trigger their own external HTTP calls. Registered as a singleton so it outlives the
/// scoped <see cref="IpDetectionService"/> (which needs a per-scope <c>AppDbContext</c>); a
/// singleton can't hold a scoped dependency directly, hence the split into its own class.
/// Only successful lookups are cached -- a failed detection isn't, so a transient blip doesn't
/// force every other caller in the same window to also fail without retrying.
/// </summary>
internal sealed class IpDetectionCache(TimeSpan? cacheDuration = null) : IDisposable
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromSeconds(25);

    private readonly TimeSpan _cacheDuration = cacheDuration ?? DefaultCacheDuration;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (string Ip, DateTime ExpiresAtUtc)? _cached;

    public async Task<IpDetectionResult> GetOrDetectAsync(
        Func<Task<IpDetectionResult>> factory, CancellationToken cancellationToken)
    {
        if (TryGetFresh(out var cachedIp))
        {
            return IpDetectionResult.Succeeded(cachedIp);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetFresh(out cachedIp))
            {
                return IpDetectionResult.Succeeded(cachedIp);
            }

            var result = await factory();
            if (result.Success && result.IpAddress is not null)
            {
                _cached = (result.IpAddress, DateTime.UtcNow.Add(_cacheDuration));
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryGetFresh(out string ip)
    {
        var snapshot = _cached;
        if (snapshot is { } value && value.ExpiresAtUtc > DateTime.UtcNow)
        {
            ip = value.Ip;
            return true;
        }

        ip = "";
        return false;
    }

    public void Dispose() => _gate.Dispose();
}
