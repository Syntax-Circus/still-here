using System.Security.Cryptography;
using System.Text.Json;
using StillHere.Application.Features.AuditLog;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Application.Features.Notifications;
using StillHere.Application.IpDetection;
using StillHere.Application.Security;

namespace StillHere.Application.Features.DomainChecks;

/// <summary>
/// The shared check/update core reused by both the scheduler tick and (via
/// <see cref="IRunManualDomainCheckRequestHandler"/>) the manual "check now" flow. No
/// <c>Result&lt;T&gt;</c> -- assumes its caller already validated the domain exists, per
/// APPLICATION_ARCHITECTURE.md's "an internal handler with no meaningful expected negative
/// outcome may return Task."
/// </summary>
public interface IRunScheduledDomainCheckHandler
{
    Task<DomainCheckOutcomeDto> HandleAsync(int managedDomainId, CancellationToken cancellationToken);
}

public sealed class RunScheduledDomainCheckHandler(
    IManagedDomainRepository managedDomains,
    IIpDetectionService ipDetection,
    IDnsProviderRegistry dnsProviders,
    ICredentialProtector credentialProtector,
    IAuditLogWriter auditLog,
    INotificationDispatcher dispatcher) : IRunScheduledDomainCheckHandler
{
    public async Task<DomainCheckOutcomeDto> HandleAsync(int managedDomainId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var domain = await managedDomains.FindForCheckAsync(managedDomainId, cancellationToken)
            ?? throw new InvalidOperationException($"ManagedDomain {managedDomainId} was not found during a check cycle.");

        var ipResult = await ipDetection.DetectCurrentIpAsync(cancellationToken);
        if (!ipResult.Success)
        {
            // No "DetectionFailed" AuditEventType member exists in the already-migrated schema;
            // Success:false on a CheckOnly entry satisfies FR-17's "success or failure" without a
            // schema change.
            await auditLog.WriteAsync(
                new WriteAuditLogEntryRequest(domain.Id, AuditEventKind.CheckOnly, domain.LastKnownIp, null, ipResult.Message, Success: false, nowUtc),
                cancellationToken);
            await managedDomains.RecordCheckResultAsync(domain.Id, DomainCheckOutcomeKind.DetectionFailed, newLastKnownIp: null, nowUtc, cancellationToken);

            return new DomainCheckOutcomeDto(domain.Id, DomainCheckOutcomeKind.DetectionFailed, domain.LastKnownIp, null, ipResult.Message, nowUtc);
        }

        var currentIp = ipResult.IpAddress!;
        var changed = domain.LastKnownIp is null || !string.Equals(domain.LastKnownIp, currentIp, StringComparison.Ordinal);

        if (!changed)
        {
            var unchangedMessage = $"IP unchanged ({currentIp}).";
            await auditLog.WriteAsync(
                new WriteAuditLogEntryRequest(domain.Id, AuditEventKind.CheckOnly, domain.LastKnownIp, currentIp, unchangedMessage, Success: true, nowUtc),
                cancellationToken);
            await managedDomains.RecordCheckResultAsync(domain.Id, DomainCheckOutcomeKind.Unchanged, newLastKnownIp: null, nowUtc, cancellationToken);

            return new DomainCheckOutcomeDto(domain.Id, DomainCheckOutcomeKind.Unchanged, domain.LastKnownIp, currentIp, unchangedMessage, nowUtc);
        }

        var changeMessage = domain.LastKnownIp is null
            ? $"First IP detected: {currentIp}."
            : $"IP changed from {domain.LastKnownIp} to {currentIp}.";
        await auditLog.WriteAsync(
            new WriteAuditLogEntryRequest(domain.Id, AuditEventKind.IpChanged, domain.LastKnownIp, currentIp, changeMessage, Success: true, nowUtc),
            cancellationToken);
        await dispatcher.DispatchAsync(
            NotificationTrigger.IpChange,
            new NotificationEventContext(domain.DomainName, domain.LastKnownIp, currentIp, "IpChanged", changeMessage),
            cancellationToken);

        DnsUpdateResult updateResult;
        try
        {
            var json = credentialProtector.Unprotect(domain.EncryptedSecrets);
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            var provider = dnsProviders.GetByKey(domain.ProviderKey);

            updateResult = await provider.UpdateAsync(
                new DnsUpdateRequest(domain.DomainName, domain.Host, secrets, currentIp), cancellationToken);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            updateResult = DnsUpdateResult.Failed($"Could not decrypt or parse stored credentials: {ex.Message}");
        }

        if (updateResult.Success)
        {
            var mismatchNote = ipDetection.CompareProviderReportedIp(currentIp, updateResult.ProviderReportedIp) == ProviderIpComparisonOutcome.Mismatch
                ? $" Provider reported IP {updateResult.ProviderReportedIp}, which differs from the IP sent."
                : "";
            var successMessage = updateResult.Message + mismatchNote;

            await auditLog.WriteAsync(
                new WriteAuditLogEntryRequest(domain.Id, AuditEventKind.UpdateSucceeded, domain.LastKnownIp, currentIp, successMessage, Success: true, nowUtc),
                cancellationToken);
            await dispatcher.DispatchAsync(
                NotificationTrigger.Success,
                new NotificationEventContext(domain.DomainName, domain.LastKnownIp, currentIp, "UpdateSucceeded", successMessage),
                cancellationToken);
            await managedDomains.RecordCheckResultAsync(domain.Id, DomainCheckOutcomeKind.Updated, currentIp, nowUtc, cancellationToken);

            return new DomainCheckOutcomeDto(domain.Id, DomainCheckOutcomeKind.Updated, domain.LastKnownIp, currentIp, successMessage, nowUtc);
        }

        await auditLog.WriteAsync(
            new WriteAuditLogEntryRequest(domain.Id, AuditEventKind.UpdateFailed, domain.LastKnownIp, currentIp, updateResult.Message, Success: false, nowUtc),
            cancellationToken);
        await dispatcher.DispatchAsync(
            NotificationTrigger.Failure,
            new NotificationEventContext(domain.DomainName, domain.LastKnownIp, currentIp, "UpdateFailed", updateResult.Message),
            cancellationToken);
        await managedDomains.RecordCheckResultAsync(domain.Id, DomainCheckOutcomeKind.UpdateFailed, newLastKnownIp: null, nowUtc, cancellationToken);

        return new DomainCheckOutcomeDto(domain.Id, DomainCheckOutcomeKind.UpdateFailed, domain.LastKnownIp, currentIp, updateResult.Message, nowUtc);
    }
}
