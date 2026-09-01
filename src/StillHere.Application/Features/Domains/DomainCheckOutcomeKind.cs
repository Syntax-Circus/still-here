namespace StillHere.Application.Features.Domains;

/// <summary>
/// Mirrors (never reuses) the internal <c>DomainCheckStatus</c> entity enum -- Application code must
/// not reference Infrastructure entities. <c>ManagedDomainRepository</c> maps this 1:1 onto
/// <c>DomainCheckStatus</c> internally.
/// </summary>
public enum DomainCheckOutcomeKind
{
    Unchanged,
    Updated,
    UpdateFailed,
    DetectionFailed,
}
