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

- [x] `/` dashboard: domain table, status badges, enabled toggle, "check now" button.
- [x] `/audit-log`: global paged, filterable audit log.
- [x] `/domains/{id}/history`: per-domain paged history with old→new IP diff view.

## Actionable Tasks

- [x] **P07-01** Implement `GetDashboardSummaryRequestHandler` and the `/` page.
  - **Depends on:** Phase 04
  - **Validation:** Handler test covers empty state; manual verification of live status badges.
- [x] **P07-02** Implement `GetAuditLogEntriesRequestHandler` with filter/paging, and the `/audit-log` and `/domains/{id}/history` pages.
  - **Depends on:** Phase 06
  - **Validation:** Handler test covers filter/paging logic; manual verification of filtering by event type/success/date range.
- [x] **P07-03** Wire the "check now" button to `RunManualDomainCheckRequestHandler` (Phase 06).
  - **Depends on:** P07-01, Phase 06
  - **Validation:** Manual verification — clicking "check now" updates the dashboard row and writes an audit entry.

## Success Criteria

- [x] `dotnet test` passes for dashboard/audit-log handler tests.
- [x] Manual verification: full flow — add a domain (Phase 04), see it on the dashboard, trigger "check now," see the result reflected and logged.

## Boundary Validation

- [x] Application use-case entry points delegate to the named handlers listed above.
- [x] Framework-owned operational or static exemptions execute no application workflow.
- [x] Handler constructor dependencies contain only approved abstractions.
- [x] Persistence and integration entities do not cross infrastructure boundaries.
- [x] Cancellation reaches asynchronous handler dependencies.
- [x] Expected outcomes and transport mapping have focused tests.
- [x] Infrastructure implementations have integration coverage where applicable.
- [x] Inline Razor components contain only simple parameters and, at most, one trivial synchronous `EventCallback`-forwarding callback.
- [x] Every component beyond the inline ceiling uses paired `.razor` and `.razor.cs` files, with all C# in code-behind.
- [x] Each Razor ViewModel is feature-local and presentation-only; the recorded direct-model decision does not expose an API ViewModel.
- [x] A factory or presentation service is used only for non-trivial mapping, asynchronous assembly, or multiple dependencies.
- [x] API request and response contracts use DTO names and contracts, never Razor ViewModels.
- [x] Repeated or business-meaningful literals are named constants at the right scope, not bare magic values.
- [x] Duplicated-looking logic across flows was evaluated for genuine divergence before extracting (or intentionally not extracting) a shared abstraction.

## Risks and Open Questions

- [x] Styling convention (Bootstrap 5 via SCSS + `AspNetCore.SassCompiler` + libman, applied to all pages including pre-existing ones) and check-now-on-disabled-domains behavior (always enabled) were resolved with the project owner before implementation.
- [x] `FirstRunGateMiddleware` (Phase 02) did not exempt static assets (CSS/JS) from its pre-admin redirect, breaking the `/setup` and `/login` pages' own styling once real CSS was wired in — fixed by adding an extension-based exemption alongside the existing prefix exemptions.
- [x] Native `<select @bind>` does not reliably round-trip a nullable `bool` in Blazor Server — the audit log's success/failure filter is bound through a string-backed field instead.

## Handoff

Phase 08 (Notifications) is independent of this phase's UI (it hooks into the Phase 06 handler directly) and can proceed in parallel. Phase 09 (Polish) depends on this phase for retention/pruning to have something to prune. Next: [PHASE-08-notifications.md](PHASE-08-notifications.md).
