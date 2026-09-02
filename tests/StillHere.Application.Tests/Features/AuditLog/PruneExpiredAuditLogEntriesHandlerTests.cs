using NSubstitute;
using Shouldly;
using StillHere.Application.Features.AuditLog;
using Xunit;

namespace StillHere.Application.Tests.Features.AuditLog;

public sealed class PruneExpiredAuditLogEntriesHandlerTests
{
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly PruneExpiredAuditLogEntriesHandler _handler;

    public PruneExpiredAuditLogEntriesHandlerTests()
    {
        _handler = new PruneExpiredAuditLogEntriesHandler(_auditLog);
    }

    [Fact]
    public async Task HandleAsync_DelegatesToRepositoryAndReturnsItsResult()
    {
        _auditLog.PruneExpiredAsync(Arg.Any<CancellationToken>()).Returns(7);

        var deletedCount = await _handler.HandleAsync(TestContext.Current.CancellationToken);

        deletedCount.ShouldBe(7);
        await _auditLog.Received(1).PruneExpiredAsync(Arg.Any<CancellationToken>());
    }
}
