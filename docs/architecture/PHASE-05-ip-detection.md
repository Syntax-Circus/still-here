# PHASE-05: IP Detection

## Objective

Implement `IIpDetectionService`: an ordered external IP-check fallback chain, shared per-tick caching, and provider-reported IP comparison.

## Dependencies

- **Depends on:** Phase 01 (solution structure, `GlobalSettings` entity for the fallback-chain configuration).
- **Unblocks:** Phase 06 (scheduler calls `IIpDetectionService`).
- **External prerequisites:** None (uses public IP-check services: ifconfig.me, api.ipify.org, icanhazip.com).

## Architecture Decisions

- One external IP lookup is shared across all due domains within a single scheduler tick (per [01-REQUIREMENTS.md](01-REQUIREMENTS.md) FR-13) — caching lives in `IIpDetectionService`, not in the scheduler itself, so "check now" (Phase 06/07) gets the same caching behavior for free within its own call.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md). `IIpDetectionService` is infrastructure, invoked by handlers introduced in Phase 06 — no new entry point in this phase.

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| — (infrastructure-only phase) | — | — | `IpDetectionService : IIpDetectionService` | — | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). No Razor components in this phase.

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — | — | — | — | — |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Http.Resilience` | Resilient outbound HTTP | Wraps each external IP-check call with short timeouts and fallback | Version locked in Phase 01 |

## Deliverables

- [x] `IIpDetectionService` interface and `IpDetectionService` implementation.
- [x] Configurable, ordered fallback chain sourced from `GlobalSettings.ExternalIpCheckServices`.
- [x] Per-tick shared-lookup caching.
- [x] Provider-reported IP comparison/mismatch-warning logic (consumes an `IDnsProvider` update result).

## Actionable Tasks

- [x] **P05-01** Define `IIpDetectionService` and implement the ordered fallback chain with short timeouts.
  - **Depends on:** Phase 01
  - **Validation:** Unit tests cover first-service-success, fallback-on-timeout, and all-services-failed branches.
- [x] **P05-02** Add per-tick caching so multiple due domains share one external lookup.
  - **Depends on:** P05-01
  - **Validation:** Unit test confirms the external HTTP client is invoked once per tick regardless of due-domain count. (Phase-05-scoped as "invoked once across N calls within the cache TTL" — "tick"/"due domain" are Phase 06 concepts that don't exist yet.)
- [x] **P05-03** Add provider-reported-IP comparison, logging mismatches as a warning-level result.
  - **Depends on:** P05-01
  - **Validation:** Unit test covers match and mismatch cases.

## Success Criteria

- [x] `dotnet test` passes for all `IIpDetectionService` unit tests.
- [x] Manual verification: with two of three fallback services unreachable (e.g. via a broken URL), detection still succeeds via the third.

## Boundary Validation

- [x] Application use-case entry points delegate to the named handlers listed above. (N/A — infrastructure-only phase, no new entry point.)
- [x] Framework-owned operational or static exemptions execute no application workflow.
- [x] Handler constructor dependencies contain only approved abstractions. (N/A — no new handler.)
- [x] Persistence and integration entities do not cross infrastructure boundaries.
- [x] Cancellation reaches asynchronous handler dependencies.
- [x] Expected outcomes and transport mapping have focused tests.
- [x] Infrastructure implementations have integration coverage where applicable.
- [x] Inline Razor components contain only simple parameters and, at most, one trivial synchronous `EventCallback`-forwarding callback. (N/A — no Razor components in this phase.)
- [x] Every component beyond the inline ceiling uses paired `.razor` and `.razor.cs` files, with all C# in code-behind. (N/A)
- [x] Each Razor ViewModel is feature-local and presentation-only; the recorded direct-model decision does not expose an API ViewModel. (N/A)
- [x] A factory or presentation service is used only for non-trivial mapping, asynchronous assembly, or multiple dependencies. (N/A)
- [x] API request and response contracts use DTO names and contracts, never Razor ViewModels. (N/A)
- [x] Repeated or business-meaningful literals are named constants at the right scope, not bare magic values. (`IpCheckHttpClientName`, `DefaultCacheDuration`.)
- [x] Duplicated-looking logic across flows was evaluated for genuine divergence before extracting (or intentionally not extracting) a shared abstraction.

## Risks and Open Questions

- [x] None specific to this phase.

## Handoff

Phase 06 (Scheduler) needs `IIpDetectionService` to exist. This phase can run in parallel with Phase 03 and Phase 04 — all three depend only on Phase 01. Next: [PHASE-06-scheduler.md](PHASE-06-scheduler.md).
