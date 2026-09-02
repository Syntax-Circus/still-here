namespace StillHere.Application.Features.Domains;

/// <summary>
/// The one domain DTO that exposes <see cref="EncryptedSecrets"/> -- <see cref="ManagedDomainDto"/>
/// stays the CRUD/UI-facing contract and is never widened for this. A check cycle needs the
/// encrypted blob to decrypt and build a provider update request.
/// </summary>
public sealed record ManagedDomainCheckDetailDto(
    int Id,
    string DomainName,
    string Host,
    string ProviderKey,
    string EncryptedSecrets,
    string? LastKnownIp);
