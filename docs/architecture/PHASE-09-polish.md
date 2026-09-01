# PHASE-09: Polish

## Objective

Finalize Serilog configuration, resilience policy tuning, audit log retention/pruning, and top-level project documentation.

## Dependencies

- **Depends on:** Phase 07 (dashboard/audit log — retention prunes this data), Phase 08 (notifications — resilience tuning covers webhook/email sends too).
- **Unblocks:** None (final phase).
- **External prerequisites:** None.

## Architecture Decisions

None beyond what earlier phases already established; this phase tunes and finalizes, it does not introduce new architecture.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| Retention pruning tick (`BackgroundService` or scheduled job, constructor-injected) | `PruneExpiredAuditLogEntriesHandler` | `IAuditLogRepository` | EF repository | `Task` (no caller branches on outcome) | Hosted-job constructor-injection exception, consistent with Phase 06's scheduler |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). No new components — this phase only finalizes existing ones.

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — | — | — | — | — |

## Syntax Circus Packages

No new packages — this phase finalizes configuration for packages already selected in [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md) (`SyntaxCircus.AspNetCore.Serilog`, `SyntaxCircus.Http.Resilience`).

## Deliverables

- [ ] Finalized Serilog configuration (console + rolling file, per [02-ARCHITECTURE.md](02-ARCHITECTURE.md) Docker section).
- [ ] Tuned retry/backoff policies for provider, IP-check, and notification HTTP calls.
- [ ] Audit log retention/pruning job honoring `GlobalSettings.AuditLogRetentionDays`.
- [ ] Repo root `README.md` with docker-compose usage instructions.

## Actionable Tasks

- [ ] **P09-01** Finalize Serilog sinks/enrichers per `SyntaxCircus.AspNetCore.Serilog` conventions.
  - **Depends on:** Phase 01
  - **Validation:** Manual verification — `docker logs` shows structured output; rolling file appears in `/data/logs`.
- [ ] **P09-02** Review and tune `SyntaxCircus.Http.Resilience` policies across Namecheap, IP-check, and webhook calls for consistent retry counts/backoff.
  - **Depends on:** Phase 03, Phase 05, Phase 08
  - **Validation:** Named constants for retry counts/backoff, not bare magic numbers (per [../APPLICATION_ARCHITECTURE.md](../APPLICATION_ARCHITECTURE.md) Constants Over Magic Values).
- [ ] **P09-03** Implement `PruneExpiredAuditLogEntriesHandler` and its scheduled trigger.
  - **Depends on:** Phase 07
  - **Validation:** Unit test confirms entries older than `AuditLogRetentionDays` are removed and `null` retention keeps everything.
- [ ] **P09-04** Write repo root `README.md`: what still-here is, docker-compose usage, first-run instructions.
  - **Depends on:** All prior phases
  - **Validation:** Manual verification — following the README from a clean checkout gets a running container.

## Success Criteria

- [ ] `dotnet test` passes for the full suite across all phases.
- [ ] `docker compose up` from a clean checkout, following the new root README, produces a working app reachable at `/`.
- [ ] Retention pruning verified to remove only entries older than the configured window.

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

- [ ] None specific to this phase.

## Handoff

This is the final v1 phase. Any post-v1 work (Cloudflare provider, multi-user, IPv6) starts a new discovery cycle per `_template/docs/ARCHITECTURE_DISCOVERY.md`, not an ad-hoc addition to this phase set.
