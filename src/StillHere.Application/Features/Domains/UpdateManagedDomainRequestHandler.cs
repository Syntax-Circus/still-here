using System.Text.Json;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Security;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Domains;

/// <summary>
/// <paramref name="CredentialSecrets"/> is optional: <see langword="null"/>, empty, or all-blank
/// values means "leave the stored credential secrets unchanged" -- the edit form never pre-fills
/// a decrypted secret, so a resubmission is only expected when the admin is actually rotating it.
/// </summary>
public sealed record UpdateManagedDomainRequest(
    int Id,
    string DomainName,
    string Host,
    bool Enabled,
    int? PollingIntervalOverrideSeconds,
    IReadOnlyDictionary<string, string>? CredentialSecrets);

public interface IUpdateManagedDomainRequestHandler
{
    Task<Result<ManagedDomainDto>> HandleAsync(UpdateManagedDomainRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateManagedDomainRequestHandler(
    IManagedDomainRepository managedDomains,
    IDnsProviderRegistry dnsProviders,
    ICredentialProtector credentialProtector) : IUpdateManagedDomainRequestHandler
{
    public async Task<Result<ManagedDomainDto>> HandleAsync(
        UpdateManagedDomainRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await managedDomains.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return Result<ManagedDomainDto>.Failure(new ResultError(
                "domain-not-found", "Domain not found.", ResultErrorKind.NotFound));
        }

        if (string.IsNullOrWhiteSpace(request.DomainName))
        {
            return Result<ManagedDomainDto>.Failure(new ResultError(
                "domain-name-required", "A domain name is required.", ResultErrorKind.Validation, nameof(request.DomainName)));
        }

        if (string.IsNullOrWhiteSpace(request.Host))
        {
            return Result<ManagedDomainDto>.Failure(new ResultError(
                "host-required", "A host is required.", ResultErrorKind.Validation, nameof(request.Host)));
        }

        string? newEncryptedSecrets = null;
        var hasSubmittedSecrets = request.CredentialSecrets is { Count: > 0 } secrets
            && secrets.Values.Any(v => !string.IsNullOrWhiteSpace(v));

        if (hasSubmittedSecrets)
        {
            // The provider itself is fixed on edit -- only credential rotation is supported here.
            var provider = dnsProviders.Providers.FirstOrDefault(p => p.ProviderKey == existing.ProviderKey);
            if (provider is null)
            {
                return Result<ManagedDomainDto>.Failure(new ResultError(
                    "unknown-provider", $"Unknown DNS provider '{existing.ProviderKey}'.", ResultErrorKind.Validation));
            }

            var fieldResult = CredentialFieldValidator.ValidateAndProject(provider, request.CredentialSecrets!);
            if (fieldResult.IsFailure)
            {
                return Result<ManagedDomainDto>.Failure(fieldResult.Errors[0], [.. fieldResult.Errors.Skip(1)]);
            }

            var json = JsonSerializer.Serialize(fieldResult.Value);
            newEncryptedSecrets = credentialProtector.Protect(json);
        }

        var updated = await managedDomains.UpdateAsync(
            request.Id,
            request.DomainName,
            request.Host,
            request.Enabled,
            request.PollingIntervalOverrideSeconds,
            newEncryptedSecrets,
            cancellationToken);

        return Result<ManagedDomainDto>.Success(updated);
    }
}
