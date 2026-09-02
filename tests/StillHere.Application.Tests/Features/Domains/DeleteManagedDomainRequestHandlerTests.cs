using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Domains;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Domains;

public sealed class DeleteManagedDomainRequestHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly DeleteManagedDomainRequestHandler _handler;

    public DeleteManagedDomainRequestHandlerTests()
    {
        _handler = new DeleteManagedDomainRequestHandler(_managedDomains);
    }

    [Fact]
    public async Task HandleAsync_DomainNotFound_ReturnsNotFound()
    {
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ManagedDomainDto?)null);

        var result = await _handler.HandleAsync(new DeleteManagedDomainRequest(1), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
        await _managedDomains.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DomainExists_DeletesAndReturnsSuccess()
    {
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ManagedDomainDto(1, "example.com", "@", "namecheap", true, null, DateTime.UtcNow));

        var result = await _handler.HandleAsync(new DeleteManagedDomainRequest(1), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _managedDomains.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }
}
