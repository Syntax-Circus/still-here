using StillHere.Application.Features.AuditLog;

namespace StillHere.Web.Components.Pages.AuditLog;

internal static class AuditLogRowViewModelFactory
{
    public static AuditLogRowViewModel Create(AuditLogEntryDto dto) => new(
        dto.Id,
        dto.ManagedDomainId,
        dto.ManagedDomainName,
        dto.TimestampUtc,
        dto.EventType,
        FormatIpDiff(dto.OldIp, dto.NewIp),
        dto.Message,
        dto.Success);

    private static string FormatIpDiff(string? oldIp, string? newIp) => (oldIp, newIp) switch
    {
        (null, null) => "—",
        (null, var next) => $"(none) → {next}",
        (var prev, var next) when prev == next => next!,
        (var prev, var next) => $"{prev} → {next}",
    };
}
