namespace StillHere.Application.Features.Domains;

public interface IManagedDomainRepository
{
    Task<ManagedDomainDto> CreateAsync(
        string domainName,
        string host,
        string providerKey,
        string credentialName,
        string encryptedSecretsJson,
        int? pollingIntervalOverrideSeconds,
        CancellationToken cancellationToken);

    Task<ManagedDomainDto?> FindByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="newEncryptedSecretsJson"/> is <see langword="null"/> when the caller wants
    /// to leave the currently-stored credential secrets unchanged (the "blank means keep existing"
    /// edit-form convention).
    /// </summary>
    Task<ManagedDomainDto> UpdateAsync(
        int id,
        string domainName,
        string host,
        bool enabled,
        int? pollingIntervalOverrideSeconds,
        string? newEncryptedSecretsJson,
        CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ManagedDomainScheduleSummaryDto>> ListEnabledSummariesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ManagedDomainSummaryDto>> ListDashboardSummariesAsync(CancellationToken cancellationToken);

    Task<ManagedDomainCheckDetailDto?> FindForCheckAsync(int id, CancellationToken cancellationToken);

    Task RecordCheckResultAsync(
        int id,
        DomainCheckOutcomeKind kind,
        string? newLastKnownIp,
        DateTime timestampUtc,
        CancellationToken cancellationToken);
}
