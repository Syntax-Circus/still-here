using NSubstitute;
using Shouldly;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Application.Security;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Domains;

public sealed class UpdateManagedDomainRequestHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly IDnsProviderRegistry _dnsProviders = Substitute.For<IDnsProviderRegistry>();
    private readonly ICredentialProtector _credentialProtector = Substitute.For<ICredentialProtector>();
    private readonly UpdateManagedDomainRequestHandler _handler;

    private static readonly IReadOnlyList<ProviderCredentialField> NamecheapFields =
        [new ProviderCredentialField("Password", "Dynamic DNS Password", IsSecret: true)];

    private static readonly ManagedDomainDto ExistingDomain =
        new(1, "example.com", "@", "namecheap", true, null, DateTime.UtcNow);

    public UpdateManagedDomainRequestHandlerTests()
    {
        _handler = new UpdateManagedDomainRequestHandler(_managedDomains, _dnsProviders, _credentialProtector);
    }

    private static UpdateManagedDomainRequest ValidRequest(IReadOnlyDictionary<string, string>? secrets = null) => new(
        1, "example.com", "@", true, null, secrets);

    [Fact]
    public async Task HandleAsync_DomainNotFound_ReturnsNotFound()
    {
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ManagedDomainDto?)null);

        var result = await _handler.HandleAsync(ValidRequest(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
    }

    [Fact]
    public async Task HandleAsync_BlankSecrets_PreservesExistingEncryptedSecrets()
    {
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingDomain);
        _managedDomains.UpdateAsync(1, "example.com", "@", true, null, null, Arg.Any<CancellationToken>())
            .Returns(ExistingDomain);

        var result = await _handler.HandleAsync(ValidRequest(new Dictionary<string, string> { ["Password"] = "" }), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _managedDomains.Received(1).UpdateAsync(1, "example.com", "@", true, null, null, Arg.Any<CancellationToken>());
        _credentialProtector.DidNotReceive().Protect(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_SubmittedSecretsMissingRequiredField_ReturnsValidationError()
    {
        var namecheap = Substitute.For<IDnsProvider>();
        namecheap.ProviderKey.Returns("namecheap");
        namecheap.CredentialFields.Returns(NamecheapFields);
        _dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[namecheap]);
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingDomain);

        var result = await _handler.HandleAsync(
            ValidRequest(new Dictionary<string, string> { ["SomeOtherKey"] = "value" }),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("Password");
    }

    [Fact]
    public async Task HandleAsync_NewSecretsSubmitted_ReEncryptsAndUpdates()
    {
        var namecheap = Substitute.For<IDnsProvider>();
        namecheap.ProviderKey.Returns("namecheap");
        namecheap.CredentialFields.Returns(NamecheapFields);
        _dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[namecheap]);
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingDomain);
        _credentialProtector.Protect(Arg.Any<string>()).Returns("new-encrypted-blob");
        _managedDomains.UpdateAsync(1, "example.com", "@", true, null, "new-encrypted-blob", Arg.Any<CancellationToken>())
            .Returns(ExistingDomain);

        var result = await _handler.HandleAsync(
            ValidRequest(new Dictionary<string, string> { ["Password"] = "new-password" }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _managedDomains.Received(1).UpdateAsync(1, "example.com", "@", true, null, "new-encrypted-blob", Arg.Any<CancellationToken>());
    }
}
