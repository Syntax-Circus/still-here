using NSubstitute;
using Shouldly;
using StillHere.Application.Features.AuditLog;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.AuditLog;

public sealed class GetAuditLogEntriesRequestHandlerTests
{
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly GetAuditLogEntriesRequestHandler _handler;

    public GetAuditLogEntriesRequestHandlerTests()
    {
        _handler = new GetAuditLogEntriesRequestHandler(_auditLog);
    }

    [Fact]
    public async Task HandleAsync_NoMatches_ReturnsEmptyPagedResult()
    {
        _auditLog.QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditLogEntryDto>([], 1, AuditLogPaging.DefaultPageSize, 0));

        var result = await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(null, null, null, null, null, 1, AuditLogPaging.DefaultPageSize),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_AllFiltersProvided_PassesThemUnchangedToRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _auditLog.QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditLogEntryDto>([], 2, 10, 0));

        await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(5, AuditEventKind.IpChanged, true, from, to, 2, 10),
            TestContext.Current.CancellationToken);

        await _auditLog.Received(1).QueryAsync(5, AuditEventKind.IpChanged, true, from, to, 2, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PageLessThanOne_ClampsToOne()
    {
        _auditLog.QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditLogEntryDto>([], 1, AuditLogPaging.DefaultPageSize, 0));

        await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(null, null, null, null, null, 0, AuditLogPaging.DefaultPageSize),
            TestContext.Current.CancellationToken);

        await _auditLog.Received(1).QueryAsync(
            null, null, null, null, null, 1, AuditLogPaging.DefaultPageSize, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, AuditLogPaging.DefaultPageSize)]
    [InlineData(-5, AuditLogPaging.DefaultPageSize)]
    [InlineData(500, AuditLogPaging.MaxPageSize)]
    public async Task HandleAsync_PageSizeOutOfRange_ClampsToDefaultOrMax(int requestedPageSize, int expectedPageSize)
    {
        _auditLog.QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditLogEntryDto>([], 1, expectedPageSize, 0));

        await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(null, null, null, null, null, 1, requestedPageSize),
            TestContext.Current.CancellationToken);

        await _auditLog.Received(1).QueryAsync(
            null, null, null, null, null, 1, expectedPageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FromUtcAfterToUtc_ReturnsValidationFailureWithoutQuerying()
    {
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(null, null, null, from, to, 1, AuditLogPaging.DefaultPageSize),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Validation);
        await _auditLog.DidNotReceive().QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepositoryReturnsPage_WrapsAsSuccessResult()
    {
        var entries = new List<AuditLogEntryDto>
        {
            new(1, 1, "example.com", DateTime.UtcNow, AuditEventKind.IpChanged, "1.2.3.4", "5.6.7.8", "changed", true),
        };
        var page = new PagedResult<AuditLogEntryDto>(entries, 1, AuditLogPaging.DefaultPageSize, 1);
        _auditLog.QueryAsync(
            Arg.Any<int?>(), Arg.Any<AuditEventKind?>(), Arg.Any<bool?>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _handler.HandleAsync(
            new GetAuditLogEntriesRequest(null, null, null, null, null, 1, AuditLogPaging.DefaultPageSize),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
    }
}
