using Microsoft.EntityFrameworkCore;
using StillHere.Application.Features.Notifications;
using Entities = StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class NotificationChannelRepository(AppDbContext db) : INotificationChannelRepository
{
    public async Task<NotificationChannelDto> CreateAsync(
        NotificationChannelType type, string name, bool enabled,
        string? url, string? bodyTemplate, string? httpMethod,
        string? smtpHost, int? smtpPort, bool useSsl, string? username, string? encryptedPassword,
        string? fromAddress, string? toAddresses,
        bool triggerOnIpChange, bool triggerOnFailure, bool triggerOnSuccess,
        CancellationToken cancellationToken)
    {
        var channel = new Entities.NotificationChannel
        {
            Type = ToEntityType(type),
            Name = name,
            Enabled = enabled,
            Url = url,
            BodyTemplate = bodyTemplate,
            HttpMethod = httpMethod,
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            UseSsl = useSsl,
            Username = username,
            EncryptedPassword = encryptedPassword,
            FromAddress = fromAddress,
            ToAddresses = toAddresses,
            TriggerOnIpChange = triggerOnIpChange,
            TriggerOnFailure = triggerOnFailure,
            TriggerOnSuccess = triggerOnSuccess,
        };

        db.NotificationChannels.Add(channel);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(channel);
    }

    public async Task<NotificationChannelDto?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        var channel = await db.NotificationChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return channel is null ? null : ToDto(channel);
    }

    public async Task<IReadOnlyList<NotificationChannelDto>> ListAllAsync(CancellationToken cancellationToken)
    {
        var channels = await db.NotificationChannels
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return [.. channels.Select(ToDto)];
    }

    public async Task<IReadOnlyList<NotificationChannelDto>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        var channels = await db.NotificationChannels
            .AsNoTracking()
            .Where(c => c.Enabled)
            .ToListAsync(cancellationToken);

        return [.. channels.Select(ToDto)];
    }

    public async Task<NotificationChannelDto> UpdateAsync(
        int id, string name, bool enabled,
        string? url, string? bodyTemplate, string? httpMethod,
        string? smtpHost, int? smtpPort, bool useSsl, string? username, string? newEncryptedPassword,
        string? fromAddress, string? toAddresses,
        bool triggerOnIpChange, bool triggerOnFailure, bool triggerOnSuccess,
        CancellationToken cancellationToken)
    {
        var channel = await db.NotificationChannels
            .FirstAsync(c => c.Id == id, cancellationToken);

        channel.Name = name;
        channel.Enabled = enabled;
        channel.Url = url;
        channel.BodyTemplate = bodyTemplate;
        channel.HttpMethod = httpMethod;
        channel.SmtpHost = smtpHost;
        channel.SmtpPort = smtpPort;
        channel.UseSsl = useSsl;
        channel.Username = username;
        channel.FromAddress = fromAddress;
        channel.ToAddresses = toAddresses;
        channel.TriggerOnIpChange = triggerOnIpChange;
        channel.TriggerOnFailure = triggerOnFailure;
        channel.TriggerOnSuccess = triggerOnSuccess;

        if (newEncryptedPassword is not null)
        {
            channel.EncryptedPassword = newEncryptedPassword;
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(channel);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var channel = await db.NotificationChannels
            .FirstAsync(c => c.Id == id, cancellationToken);

        db.NotificationChannels.Remove(channel);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Entities.NotificationChannelType ToEntityType(NotificationChannelType type) => type switch
    {
        NotificationChannelType.Webhook => Entities.NotificationChannelType.Webhook,
        NotificationChannelType.Email => Entities.NotificationChannelType.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static NotificationChannelType ToApplicationType(Entities.NotificationChannelType type) => type switch
    {
        Entities.NotificationChannelType.Webhook => NotificationChannelType.Webhook,
        Entities.NotificationChannelType.Email => NotificationChannelType.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static NotificationChannelDto ToDto(Entities.NotificationChannel channel) => new(
        channel.Id,
        ToApplicationType(channel.Type),
        channel.Name,
        channel.Enabled,
        channel.Url,
        channel.BodyTemplate,
        channel.HttpMethod,
        channel.SmtpHost,
        channel.SmtpPort,
        channel.UseSsl,
        channel.Username,
        channel.EncryptedPassword,
        channel.FromAddress,
        channel.ToAddresses,
        channel.TriggerOnIpChange,
        channel.TriggerOnFailure,
        channel.TriggerOnSuccess);
}
