# PHASE-06: Scheduler

## Objective

Implement the `BackgroundService` scheduler and the shared check/update handler it (and manual "check now") both call.

## Dependencies

- **Depends on:** Phase 03 (`IDnsProvider`), Phase 04 (`ManagedDomain` CRUD), Phase 05 (`IIpDetectionService`).
- **Unblocks:** Phase 07 (dashboard/audit UI shows scheduler output), Phase 08 (notifications fan out from this handler).
- **External prerequisites:** None.

## Architecture Decisions

- The scheduler tick and the manual "check now" button both invoke the same handler logic (`RunScheduledDomainCheckHandler` for the tick, `RunManualDomainCheckRequestHandler` for the button) rather than duplicating check/update logic — per [02-ARCHITECTURE.md](02-ARCHITECTURE.md) data flow #3.
- Per the template's hosted-job exception, the scheduler constructor-injects its handler dependency rather than using `[FromServices]`.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| Scheduler tick (`BackgroundService`, constructor-injected) | `RunScheduledDomainCheckHandler` | `IManagedDomainRepository`, `IIpDetectionService`, `IDnsProviderRegistry`, `IAuditLogWriter`, `INotificationDispatcher` | EF repository, `IDnsProvider` impls, HTTP clients | `Task` (fire-and-forget per domain) | Constructor injection is the template's stated hosted-job exception, not a deviation |
| "Check now" button | `RunManualDomainCheckRequestHandler` | Same as above | Same as above | `Result<DomainCheckOutcomeDto>` | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). "Check now" button ships in Phase 07 alongside the dashboard; this phase only implements the handler it calls.

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — (deferred to Phase 07) | — | — | — | — |

## Syntax Circus Packages

No new packages in this phase — reuses `SyntaxCircus.Common` (`Result`), `SyntaxCircus.Http.Resilience` (already wired into `IDnsProvider`/`IIpDetectionService` in Phases 03/05).

## Deliverables

- [ ] `RunScheduledDomainCheckHandler` and `RunManualDomainCheckRequestHandler` sharing the same check/update logic.
- [ ] `BackgroundService` scheduler with `PeriodicTimer`, due-domain selection by effective interval.
- [ ] Audit log writes for `CheckOnly`, `IpChanged`, `UpdateFailed`, `UpdateSucceeded`.

## Actionable Tasks

- [ ] **P06-01** Implement due-domain selection logic (`PollingIntervalOverrideSeconds ?? GlobalSettings.DefaultPollingIntervalSeconds`).
  - **Depends on:** Phase 04
  - **Validation:** Unit tests cover interval math and override precedence.
- [ ] **P06-02** Implement the shared check/update logic as `RunScheduledDomainCheckHandler`/`RunManualDomainCheckRequestHandler`, covering unchanged/changed/failure branches.
  - **Depends on:** P06-01, Phase 03, Phase 05
  - **Validation:** Handler tests substitute `IDnsProviderRegistry`/`IIpDetectionService`/repository; cover all three branches.
- [ ] **P06-03** Implement the `BackgroundService` tick, constructor-injecting the handler.
  - **Depends on:** P06-02
  - **Validation:** Integration-style test against a temp SQLite DB covers the full check→update→audit pipeline.
- [ ] **P06-04** Add a rate-limit decision for "check now" (resolve open question).
  - **Depends on:** P06-02
  - **Validation:** Decision recorded in this phase doc or a follow-up decision-log entry if it constitutes a boundary deviation (it does not — implementation detail only).

## Success Criteria

- [ ] `dotnet test` passes for due-domain selection, check/update branch, and full-pipeline integration tests.
- [ ] Manual verification: scheduler ticks, checks a real (or stubbed) domain, and writes the expected audit entries.

## Boundary Validation

- [ ] Application use-case entry points delegate to the named handlers listed above.
- [ ] Framework-owned operational or static exemptions execute no application workflow.
- [ ] Handler constructor dependencies contain only approved abstractions.
- [ ] Persistence and integration entities do not cross infrastructure boundaries.
- [ ] Cancellation reaches asynchronous handler dependencies.
- [ ] Expected outcomes and transport mapping have focused tests.
- [ ] Infrastructure implementations have integration coverage where applicable.
- [ ] Inline Razor components contain only simple parameters and, at most, one trivial synchronous `EventCallback`-forwarding callback.
- [ ] Every component beyond the inline ceiling uses paired `.razor` and `.razor.cs` files, with all C# in code-behind.
- [ ] Each Razor ViewModel is feature-local and presentation-only; the recorded direct-model decision does not expose an API ViewModel.
- [ ] A factory or presentation service is used only for non-trivial mapping, asynchronous assembly, or multiple dependencies.
- [ ] API request and response contracts use DTO names and contracts, never Razor ViewModels.
- [ ] Repeated or business-meaningful literals are named constants at the right scope, not bare magic values.
- [ ] Duplicated-looking logic across flows was evaluated for genuine divergence before extracting (or intentionally not extracting) a shared abstraction.

## Risks and Open Questions

- [ ] Whether "check now" should be rate-limited — resolved in P06-04.

## Handoff

Phase 07 needs this handler to power the dashboard and "check now" button; Phase 08 needs `INotificationDispatcher` fan-out hooked into this handler. Next: [PHASE-07-dashboard-audit-log.md](PHASE-07-dashboard-audit-log.md).
