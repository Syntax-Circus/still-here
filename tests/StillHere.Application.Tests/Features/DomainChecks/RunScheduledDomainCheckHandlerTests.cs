using System.Security.Cryptography;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using StillHere.Application.Features.AuditLog;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.DomainChecks;
using StillHere.Application.Features.Domains;
using StillHere.Application.IpDetection;
using StillHere.Application.Security;
using Xunit;

namespace StillHere.Application.Tests.Features.DomainChecks;

public sealed class RunScheduledDomainCheckHandlerTests
{
    private readonly IManagedDomainRepository _managedDomains = Substitute.For<IManagedDomainRepository>();
    private readonly IIpDetectionService _ipDetection = Substitute.For<IIpDetectionService>();
    private readonly IDnsProviderRegistry _dnsProviders = Substitute.For<IDnsProviderRegistry>();
    private readonly ICredentialProtector _credentialProtector = Substitute.For<ICredentialProtector>();
    private readonly IAuditLogWriter _auditLog = Substitute.For<IAuditLogWriter>();
    private readonly IDnsProvider _provider = Substitute.For<IDnsProvider>();
    private readonly RunScheduledDomainCheckHandler _handler;

    public RunScheduledDomainCheckHandlerTests()
    {
        _handler = new RunScheduledDomainCheckHandler(_managedDomains, _ipDetection, _dnsProviders, _credentialProtector, _auditLog);

        _dnsProviders.GetByKey("namecheap").Returns(_provider);
        _credentialProtector.Unprotect(Arg.Any<string>()).Returns("{}");
    }

    private static ManagedDomainCheckDetailDto Domain(string? lastKnownIp) =>
        new(1, "example.com", "@", "namecheap", "encrypted-secret", lastKnownIp);

    [Fact]
    public async Task HandleAsync_UnchangedIp_WritesCheckOnlyAndRecordsUnchanged()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.2.3.4"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("1.2.3.4"));

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.Unchanged);
        await _auditLog.Received(1).WriteAsync(
            Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.CheckOnly && r.Success),
            Arg.Any<CancellationToken>());
        await _managedDomains.Received(1).RecordCheckResultAsync(1, DomainCheckOutcomeKind.Unchanged, null, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _provider.DidNotReceive().UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FirstRunWithUpdateSuccess_WritesIpChangedThenUpdateSucceeded()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain(null));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("9.9.9.9"));
        _provider.UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(DnsUpdateResult.Succeeded("9.9.9.9", "Updated."));
        _ipDetection.CompareProviderReportedIp("9.9.9.9", "9.9.9.9").Returns(ProviderIpComparisonOutcome.Match);

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.Updated);
        outcome.NewIp.ShouldBe("9.9.9.9");
        await _auditLog.Received(1).WriteAsync(Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.IpChanged), Arg.Any<CancellationToken>());
        await _auditLog.Received(1).WriteAsync(Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.UpdateSucceeded), Arg.Any<CancellationToken>());
        await _managedDomains.Received(1).RecordCheckResultAsync(1, DomainCheckOutcomeKind.Updated, "9.9.9.9", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChangedIpWithUpdateSuccess_WritesIpChangedThenUpdateSucceeded()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.1.1.1"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("2.2.2.2"));
        _provider.UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(DnsUpdateResult.Succeeded("2.2.2.2", "Updated."));
        _ipDetection.CompareProviderReportedIp("2.2.2.2", "2.2.2.2").Returns(ProviderIpComparisonOutcome.Match);

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.Updated);
        outcome.OldIp.ShouldBe("1.1.1.1");
        outcome.NewIp.ShouldBe("2.2.2.2");
    }

    [Fact]
    public async Task HandleAsync_ChangedIpWithUpdateFailure_WritesIpChangedThenUpdateFailedAndLeavesLastKnownIpUnchanged()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.1.1.1"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("2.2.2.2"));
        _provider.UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(DnsUpdateResult.Failed("Provider rejected the update."));

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.UpdateFailed);
        await _auditLog.Received(1).WriteAsync(Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.IpChanged), Arg.Any<CancellationToken>());
        await _auditLog.Received(1).WriteAsync(Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.UpdateFailed && !r.Success), Arg.Any<CancellationToken>());
        await _managedDomains.Received(1).RecordCheckResultAsync(1, DomainCheckOutcomeKind.UpdateFailed, null, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DetectionFailure_WritesCheckOnlyFailureAndNeverTouchesProvider()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.1.1.1"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Failed("All services down."));

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.DetectionFailed);
        await _auditLog.Received(1).WriteAsync(Arg.Is<WriteAuditLogEntryRequest>(r => r.EventType == AuditEventKind.CheckOnly && !r.Success), Arg.Any<CancellationToken>());
        await _managedDomains.Received(1).RecordCheckResultAsync(1, DomainCheckOutcomeKind.DetectionFailed, null, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        _dnsProviders.DidNotReceive().GetByKey(Arg.Any<string>());
        await _provider.DidNotReceive().UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DecryptFailure_RoutesToUpdateFailedWithoutCallingProvider()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.1.1.1"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("2.2.2.2"));
        _credentialProtector.Unprotect(Arg.Any<string>()).Throws(new CryptographicException("bad key"));

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(DomainCheckOutcomeKind.UpdateFailed);
        outcome.Message.ShouldContain("Could not decrypt");
        await _provider.DidNotReceive().UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderReportedIpMismatch_MessageIncludesMismatchNote()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns(Domain("1.1.1.1"));
        _ipDetection.DetectCurrentIpAsync(Arg.Any<CancellationToken>()).Returns(IpDetectionResult.Succeeded("2.2.2.2"));
        _provider.UpdateAsync(Arg.Any<DnsUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(DnsUpdateResult.Succeeded("3.3.3.3", "Updated."));
        _ipDetection.CompareProviderReportedIp("2.2.2.2", "3.3.3.3").Returns(ProviderIpComparisonOutcome.Mismatch);

        var outcome = await _handler.HandleAsync(1, TestContext.Current.CancellationToken);

        outcome.Message.ShouldContain("differs from the IP sent");
    }

    [Fact]
    public async Task HandleAsync_DomainNotFound_Throws()
    {
        _managedDomains.FindForCheckAsync(1, Arg.Any<CancellationToken>()).Returns((ManagedDomainCheckDetailDto?)null);

        await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleAsync(1, TestContext.Current.CancellationToken));
    }
}
