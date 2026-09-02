# PHASE-06: Scheduler

## Objective

Implement the `BackgroundService` scheduler and the shared check/update handler it (and manual "check now") both call.

## Dependencies

- **Depends on:** Phase 03 (`IDnsProvider`), Phase 04 (`ManagedDomain` CRUD), Phase 05 (`IIpDetectionService`).
- **Unblocks:** Phase 07 (dashboard/audit UI shows scheduler output), Phase 08 (notifications fan out from this handler).
- **External prerequisites:** None.

## Architecture Decisions

- The scheduler tick and the manual "check now" button both invoke the same handler logic (`RunScheduledDomainCheckHandler` for the tick, `RunManualDomainCheckRequestHandler` for the button, the latter delegating to the former directly) rather than duplicating check/update logic — per [02-ARCHITECTURE.md](02-ARCHITECTURE.md) data flow #3.
- The scheduler constructor-injects `IServiceScopeFactory`, not the handler itself, and resolves scoped dependencies (`IListDueDomainsHandler`, `IRunScheduledDomainCheckHandler`) from a fresh `IServiceScope` each tick. `DomainCheckScheduler` is a singleton-lifetime hosted service; its handlers transitively need a `Scoped` `AppDbContext`, so directly constructor-injecting them would be a captive-dependency error. This still satisfies the template's stated hosted-job exception ("host types without per-method DI... keep constructor injection") — `IServiceScopeFactory` is what's constructor-injected. See [04-DECISION-LOG.md](04-DECISION-LOG.md) Decision 6.
- `IListDueDomainsHandler` was added beyond this doc's original two named handlers: the scheduler tick, as an entry point, must not itself coordinate two repository/provider reads (`GlobalSettings` + the domain listing) per `APPLICATION_ARCHITECTURE.md`'s "entry points must not... coordinate multiple repositories or providers."

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| Scheduler tick (`BackgroundService`, constructor-injects `IServiceScopeFactory`) | `IListDueDomainsHandler` then `RunScheduledDomainCheckHandler` per due domain | `IManagedDomainRepository`, `IGlobalSettingsReader`, `IIpDetectionService`, `IDnsProviderRegistry`, `ICredentialProtector`, `IAuditLogWriter` | EF repository, `IDnsProvider` impls, HTTP clients | `Task<DomainCheckOutcomeDto>` per domain (scheduler doesn't branch on it — each check's own audit write is the record of what happened) | See Decision 6 |
| "Check now" button | `RunManualDomainCheckRequestHandler` (delegates to `RunScheduledDomainCheckHandler`) | `IManagedDomainRepository`, `IRunScheduledDomainCheckHandler` | Same as above | `Result<DomainCheckOutcomeDto>` | — |

`INotificationDispatcher` does not appear above — it doesn't exist yet and isn't wired in until Phase 08 (see that phase's own P08-04: "Wire `INotificationDispatcher` fan-out into the Phase 06 handler"). It was listed here prematurely in earlier planning.

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). "Check now" button ships in Phase 07 alongside the dashboard; this phase only implements the handler it calls.

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — (deferred to Phase 07) | — | — | — | — |

## Syntax Circus Packages

No new packages in this phase — reuses `SyntaxCircus.Common` (`Result`), `SyntaxCircus.Http.Resilience` (already wired into `IDnsProvider`/`IIpDetectionService` in Phases 03/05).

## Deliverables

- [x] `RunScheduledDomainCheckHandler` and `RunManualDomainCheckRequestHandler` sharing the same check/update logic.
- [x] `BackgroundService` scheduler built on `SyntaxCircus.Common.PeriodicBackgroundService` (an existing locked dependency; substituted for a hand-rolled `PeriodicTimer` loop as a pure implementation detail), due-domain selection by effective interval.
- [x] Audit log writes for `CheckOnly`, `IpChanged`, `UpdateFailed`, `UpdateSucceeded`.

## Actionable Tasks

- [x] **P06-01** Implement due-domain selection logic (`PollingIntervalOverrideSeconds ?? GlobalSettings.DefaultPollingIntervalSeconds`).
  - **Depends on:** Phase 04
  - **Validation:** Unit tests cover interval math and override precedence.
- [x] **P06-02** Implement the shared check/update logic as `RunScheduledDomainCheckHandler`/`RunManualDomainCheckRequestHandler`, covering unchanged/changed/failure branches.
  - **Depends on:** P06-01, Phase 03, Phase 05
  - **Validation:** Handler tests substitute `IDnsProviderRegistry`/`IIpDetectionService`/repository; cover all three branches.
- [x] **P06-03** Implement the `BackgroundService` tick, constructor-injecting `IServiceScopeFactory` and resolving the handler per tick from a fresh scope (see Decision 6).
  - **Depends on:** P06-02
  - **Validation:** Integration-style test against a temp SQLite DB covers the full check→update→audit pipeline, plus a dedicated test proving the tick migrates a fresh unmigrated database itself.
- [x] **P06-04** Add a rate-limit decision for "check now" (resolve open question).
  - **Depends on:** P06-02
  - **Validation:** **Resolved: no rate limit.** Single-admin tool with no multi-tenant abuse surface; `IIpDetectionService`'s own cache already prevents hammering external IP-check services even if the button is mashed; the provider update call already has retry/circuit-breaker protection via `SyntaxCircus.Http.Resilience`. Not a boundary deviation, so no Decision Log entry.

## Success Criteria

- [x] `dotnet test` passes for due-domain selection, check/update branch, and full-pipeline integration tests.
- [x] Manual verification: scheduler ticks, checks a real domain (deliberately wrong Namecheap credentials, to safely observe `UpdateFailed` against real infrastructure), and writes the expected `IpChanged`/`UpdateFailed` audit entries every tick.

## Boundary Validation

- [x] Application use-case entry points delegate to the named handlers listed above.
- [x] Framework-owned operational or static exemptions execute no application workflow.
- [x] Handler constructor dependencies contain only approved abstractions.
- [x] Persistence and integration entities do not cross infrastructure boundaries. (`DomainCheckOutcomeKind`/`AuditEventKind` mirror, never reuse, the internal `DomainCheckStatus`/`AuditEventType` entity enums.)
- [x] Cancellation reaches asynchronous handler dependencies.
- [x] Expected outcomes and transport mapping have focused tests.
- [x] Infrastructure implementations have integration coverage where applicable.
- [x] Inline Razor components contain only simple parameters and, at most, one trivial synchronous `EventCallback`-forwarding callback. (N/A — no Razor components in this phase.)
- [x] Every component beyond the inline ceiling uses paired `.razor` and `.razor.cs` files, with all C# in code-behind. (N/A)
- [x] Each Razor ViewModel is feature-local and presentation-only; the recorded direct-model decision does not expose an API ViewModel. (N/A)
- [x] A factory or presentation service is used only for non-trivial mapping, asynchronous assembly, or multiple dependencies. (N/A)
- [x] API request and response contracts use DTO names and contracts, never Razor ViewModels. (N/A)
- [x] Repeated or business-meaningful literals are named constants at the right scope, not bare magic values. (`DomainCheckScheduler.DefaultTickIntervalSeconds`, `IpDetectionService.IpCheckHttpClientName`.)
- [x] Duplicated-looking logic across flows was evaluated for genuine divergence before extracting (or intentionally not extracting) a shared abstraction. (`RunManualDomainCheckRequestHandler` reuses `RunScheduledDomainCheckHandler` directly rather than duplicating check/update logic.)

## Risks and Open Questions

- [x] Whether "check now" should be rate-limited — resolved in P06-04: no rate limit.

## Handoff

Phase 07 needs this handler to power the dashboard and "check now" button; Phase 08 needs `INotificationDispatcher` fan-out hooked into this handler. Next: [PHASE-07-dashboard-audit-log.md](PHASE-07-dashboard-audit-log.md).
