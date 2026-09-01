namespace StillHere.Application.Features.DnsProviders;

/// <summary>
/// <paramref name="CredentialSecrets"/> are already decrypted -- decryption is Phase 04's concern
/// (via SyntaxCircus.Credentials), not this abstraction's.
/// </summary>
public sealed record DnsUpdateRequest(
    string DomainName,
    string Host,
    IReadOnlyDictionary<string, string> CredentialSecrets,
    string NewIp);
