using NSubstitute;
using Shouldly;
using StillHere.Application.Features.DomainChecks;
using StillHere.Application.Features.Domains;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.DomainChecks;

public sealed class RunManualDomainCheckRequestHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly IRunScheduledDomainCheckHandler _scheduledCheck = Substitute.For<IRunScheduledDomainCheckHandler>();
    private readonly RunManualDomainCheckRequestHandler _handler;

    public RunManualDomainCheckRequestHandlerTests()
    {
        _handler = new RunManualDomainCheckRequestHandler(_managedDomains, _scheduledCheck);
    }

    [Fact]
    public async Task HandleAsync_UnknownDomain_ReturnsNotFoundWithoutDelegating()
    {
        _managedDomains.FindByIdAsync(999, Arg.Any<CancellationToken>()).Returns((ManagedDomainDto?)null);

        var result = await _handler.HandleAsync(new ManualDomainCheckRequest(999), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
        await _scheduledCheck.DidNotReceive().HandleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KnownDomain_DelegatesAndWrapsOutcomeAsSuccess()
    {
        _managedDomains.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ManagedDomainDto(1, "example.com", "@", "namecheap", true, null, DateTime.UtcNow));
        var outcome = new DomainCheckOutcomeDto(1, DomainCheckOutcomeKind.Unchanged, "1.2.3.4", "1.2.3.4", "IP unchanged.", DateTime.UtcNow);
        _scheduledCheck.HandleAsync(1, Arg.Any<CancellationToken>()).Returns(outcome);

        var result = await _handler.HandleAsync(new ManualDomainCheckRequest(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(outcome);
        await _scheduledCheck.Received(1).HandleAsync(1, Arg.Any<CancellationToken>());
    }
}
