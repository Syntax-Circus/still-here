using NSubstitute;
using Shouldly;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Application.Security;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Domains;

public sealed class AddManagedDomainRequestHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly IDnsProviderRegistry _dnsProviders = Substitute.For<IDnsProviderRegistry>();
    private readonly ICredentialProtector _credentialProtector = Substitute.For<ICredentialProtector>();
    private readonly AddManagedDomainRequestHandler _handler;

    private static readonly IReadOnlyList<ProviderCredentialField> NamecheapFields =
        [new ProviderCredentialField("Password", "Dynamic DNS Password", IsSecret: true)];

    public AddManagedDomainRequestHandlerTests()
    {
        _handler = new AddManagedDomainRequestHandler(_managedDomains, _dnsProviders, _credentialProtector);
    }

    private static AddManagedDomainRequest ValidRequest(IReadOnlyDictionary<string, string>? secrets = null) => new(
        "example.com",
        "@",
        "namecheap",
        "example.com credential",
        secrets ?? new Dictionary<string, string> { ["Password"] = "ddns-password" },
        PollingIntervalOverrideSeconds: null);

    [Fact]
    public async Task HandleAsync_EmptyDomainName_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(ValidRequest() with { DomainName = "" }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("DomainName");
    }

    [Fact]
    public async Task HandleAsync_EmptyHost_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(ValidRequest() with { Host = "" }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("Host");
    }

    [Fact]
    public async Task HandleAsync_UnknownProviderKey_ReturnsValidationError()
    {
        _dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[]);

        var result = await _handler.HandleAsync(ValidRequest() with { ProviderKey = "unknown" }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Validation);
    }

    [Fact]
    public async Task HandleAsync_MissingRequiredCredentialField_ReturnsValidationError()
    {
        var namecheap = Substitute.For<IDnsProvider>();
        namecheap.ProviderKey.Returns("namecheap");
        namecheap.CredentialFields.Returns(NamecheapFields);
        _dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[namecheap]);

        var result = await _handler.HandleAsync(
            ValidRequest(new Dictionary<string, string>()),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("Password");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_EncryptsSecretsAndCreatesDomain()
    {
        var namecheap = Substitute.For<IDnsProvider>();
        namecheap.ProviderKey.Returns("namecheap");
        namecheap.CredentialFields.Returns(NamecheapFields);
        _dnsProviders.Providers.Returns((IReadOnlyList<IDnsProvider>)[namecheap]);

        _credentialProtector.Protect(Arg.Any<string>()).Returns("encrypted-blob");
        _managedDomains.CreateAsync(
                "example.com", "@", "namecheap", "example.com credential", "encrypted-blob", null, Arg.Any<CancellationToken>())
            .Returns(new ManagedDomainDto(1, "example.com", "@", "namecheap", true, null, DateTime.UtcNow));

        var result = await _handler.HandleAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DomainName.ShouldBe("example.com");
        _credentialProtector.Received(1).Protect(Arg.Is<string>(s => s.Contains("ddns-password")));
    }
}
