using Microsoft.EntityFrameworkCore;
using StillHere.Application.Features.Settings;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class GlobalSettingsReader(AppDbContext db) : IGlobalSettingsReader
{
    public async Task<int> GetDefaultPollingIntervalSecondsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.GlobalSettings.AsNoTracking()
            .FirstAsync(s => s.Id == GlobalSettings.SingletonId, cancellationToken);

        return settings.DefaultPollingIntervalSeconds;
    }
}
