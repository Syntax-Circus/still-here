namespace StillHere.Application.Features.Notifications;

public interface INotificationChannelRepository
{
    Task<NotificationChannelDto> CreateAsync(
        NotificationChannelType type, string name, bool enabled,
        string? url, string? bodyTemplate, string? httpMethod,
        string? smtpHost, int? smtpPort, bool useSsl, string? username, string? encryptedPassword,
        string? fromAddress, string? toAddresses,
        bool triggerOnIpChange, bool triggerOnFailure, bool triggerOnSuccess,
        CancellationToken cancellationToken);

    Task<NotificationChannelDto?> FindByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationChannelDto>> ListAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationChannelDto>> ListEnabledAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="newEncryptedPassword"/> is <see langword="null"/> to leave the currently
    /// stored password unchanged (mirrors IManagedDomainRepository.UpdateAsync's "blank means
    /// keep existing" edit-form convention).
    /// </summary>
    Task<NotificationChannelDto> UpdateAsync(
        int id, string name, bool enabled,
        string? url, string? bodyTemplate, string? httpMethod,
        string? smtpHost, int? smtpPort, bool useSsl, string? username, string? newEncryptedPassword,
        string? fromAddress, string? toAddresses,
        bool triggerOnIpChange, bool triggerOnFailure, bool triggerOnSuccess,
        CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
