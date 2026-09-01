# PHASE-07: Dashboard and Audit Log UI

## Objective

Implement the dashboard (`/`), global audit log (`/audit-log`), and per-domain history (`/domains/{id}/history`), including the "check now" button.

## Dependencies

- **Depends on:** Phase 04 (domain CRUD), Phase 06 (scheduler/check handlers).
- **Unblocks:** Phase 09 (polish, e.g. retention pruning, references this UI's data).
- **External prerequisites:** None.

## Architecture Decisions

None beyond what [02-ARCHITECTURE.md](02-ARCHITECTURE.md) already specifies for these pages.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| Dashboard load | `GetDashboardSummaryRequestHandler` | `IManagedDomainRepository` | EF repository | `Result<DashboardSummaryDto>` | — |
| "Check now" button | `RunManualDomainCheckRequestHandler` *(implemented Phase 06)* | — | — | — | Reused, not reimplemented |
| Audit log / domain history load | `GetAuditLogEntriesRequestHandler` | `IAuditLogRepository` | EF repository | `Result<PagedResult<AuditLogEntryDto>>` | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| `/` Dashboard | Paired | `DashboardRowViewModel` per domain; factory for domain→row mapping and status badge logic | Component owns loading/empty/error state | N/A |
| `/domains/{id}/history` | Paired | `AuditLogRowViewModel` | Component owns paging state | N/A |
| `/audit-log` | Paired | `AuditLogRowViewModel` (shared with history) | Component owns filter/paging state | N/A |
| Status badge | Inline (simple `Status` param, no callback) | — | Stateless | N/A |
| "Check now" button | Inline (simple param + one trivial `EventCallback` forwarder) | — | Stateless | N/A |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Blazor.Components` | Reconnect/error boundary UI | Dashboard is the primary long-lived Blazor Server circuit page | Version locked in Phase 01 |

## Deliverables

- [ ] `/` dashboard: domain table, status badges, enabled toggle, "check now" button.
- [ ] `/audit-log`: global paged, filterable audit log.
- [ ] `/domains/{id}/history`: per-domain paged history with old→new IP diff view.

## Actionable Tasks

- [ ] **P07-01** Implement `GetDashboardSummaryRequestHandler` and the `/` page.
  - **Depends on:** Phase 04
  - **Validation:** Handler test covers empty state; manual verification of live status badges.
- [ ] **P07-02** Implement `GetAuditLogEntriesRequestHandler` with filter/paging, and the `/audit-log` and `/domains/{id}/history` pages.
  - **Depends on:** Phase 06
  - **Validation:** Handler test covers filter/paging logic; manual verification of filtering by event type/success/date range.
- [ ] **P07-03** Wire the "check now" button to `RunManualDomainCheckRequestHandler` (Phase 06).
  - **Depends on:** P07-01, Phase 06
  - **Validation:** Manual verification — clicking "check now" updates the dashboard row and writes an audit entry.

## Success Criteria

- [ ] `dotnet test` passes for dashboard/audit-log handler tests.
- [ ] Manual verification: full flow — add a domain (Phase 04), see it on the dashboard, trigger "check now," see the result reflected and logged.

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

Phase 08 (Notifications) is independent of this phase's UI (it hooks into the Phase 06 handler directly) and can proceed in parallel. Phase 09 (Polish) depends on this phase for retention/pruning to have something to prune. Next: [PHASE-08-notifications.md](PHASE-08-notifications.md).
