using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Dashboard;
using StillHere.Application.Features.Domains;
using Xunit;

namespace StillHere.Application.Tests.Features.Dashboard;

public sealed class GetDashboardSummaryRequestHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly GetDashboardSummaryRequestHandler _handler;

    public GetDashboardSummaryRequestHandlerTests()
    {
        _handler = new GetDashboardSummaryRequestHandler(_managedDomains);
    }

    [Fact]
    public async Task HandleAsync_NoDomains_ReturnsSuccessWithEmptyList()
    {
        _managedDomains.ListDashboardSummariesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ManagedDomainSummaryDto>());

        var result = await _handler.HandleAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Domains.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_MultipleDomainsWithMixedEnabledAndStatuses_ReturnsAllMappedInOrder()
    {
        var domains = new List<ManagedDomainSummaryDto>
        {
            new(1, "a.com", "@", "namecheap", true, null, "1.2.3.4", DateTime.UtcNow, DateTime.UtcNow, ManagedDomainStatus.Ok),
            new(2, "b.com", "@", "namecheap", false, 600, null, null, null, ManagedDomainStatus.Unknown),
            new(3, "c.com", "@", "namecheap", true, null, "5.6.7.8", DateTime.UtcNow, null, ManagedDomainStatus.Failed),
        };
        _managedDomains.ListDashboardSummariesAsync(Arg.Any<CancellationToken>()).Returns(domains);

        var result = await _handler.HandleAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Domains.ShouldBe(domains);
    }
}
