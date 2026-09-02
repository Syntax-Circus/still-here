namespace StillHere.Application.Features.Settings;

/// <summary>
/// Deliberately narrow -- exposes only what the scheduler needs today, matching every other
/// repository's use-case-driven convention. A future settings-management phase can widen this.
/// </summary>
public interface IGlobalSettingsReader
{
    Task<int> GetDefaultPollingIntervalSecondsAsync(CancellationToken cancellationToken);
}
