using System.Text.Json;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Security;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Domains;

public sealed record AddManagedDomainRequest(
    string DomainName,
    string Host,
    string ProviderKey,
    string CredentialName,
    IReadOnlyDictionary<string, string> CredentialSecrets,
    int? PollingIntervalOverrideSeconds);

public interface IAddManagedDomainRequestHandler
{
    Task<Result<ManagedDomainDto>> HandleAsync(AddManagedDomainRequest request, CancellationToken cancellationToken);
}

public sealed class AddManagedDomainRequestHandler(
    IManagedDomainRepository managedDomains,
    IDnsProviderRegistry dnsProviders,
    ICredentialProtector credentialProtector) : IAddManagedDomainRequestHandler
{
    public async Task<Result<ManagedDomainDto>> HandleAsync(
        AddManagedDomainRequest request,
        CancellationToken cancellationToken)
    {
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

        var provider = dnsProviders.Providers.FirstOrDefault(p => p.ProviderKey == request.ProviderKey);
        if (provider is null)
        {
            return Result<ManagedDomainDto>.Failure(new ResultError(
                "unknown-provider", $"Unknown DNS provider '{request.ProviderKey}'.", ResultErrorKind.Validation, nameof(request.ProviderKey)));
        }

        var fieldResult = CredentialFieldValidator.ValidateAndProject(provider, request.CredentialSecrets);
        if (fieldResult.IsFailure)
        {
            return Result<ManagedDomainDto>.Failure(fieldResult.Errors[0], [.. fieldResult.Errors.Skip(1)]);
        }

        var json = JsonSerializer.Serialize(fieldResult.Value);
        var encryptedSecrets = credentialProtector.Protect(json);

        var created = await managedDomains.CreateAsync(
            request.DomainName,
            request.Host,
            request.ProviderKey,
            request.CredentialName,
            encryptedSecrets,
            request.PollingIntervalOverrideSeconds,
            cancellationToken);

        return Result<ManagedDomainDto>.Success(created);
    }
}
