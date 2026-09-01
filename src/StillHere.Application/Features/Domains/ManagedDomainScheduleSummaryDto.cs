namespace StillHere.Application.Features.Domains;

public sealed record ManagedDomainScheduleSummaryDto(
    int Id,
    int? PollingIntervalOverrideSeconds,
    DateTime? LastCheckedAtUtc);
