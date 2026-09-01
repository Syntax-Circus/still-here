using StillHere.Application.Features.Domains;

namespace StillHere.Application.Features.DomainChecks;

public sealed record DomainCheckOutcomeDto(
    int ManagedDomainId,
    DomainCheckOutcomeKind Kind,
    string? OldIp,
    string? NewIp,
    string Message,
    DateTime TimestampUtc);
