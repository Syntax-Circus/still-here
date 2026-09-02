# PHASE-08: Notifications

## Objective

Implement notification channel CRUD (`/settings`), webhook and email senders, and fan-out from the scheduler/manual-check handler.

## Dependencies

- **Depends on:** Phase 06 (scheduler/check handler to fan out from).
- **Unblocks:** Phase 09 (polish covers notification-adjacent config like retention).
- **External prerequisites:** None.

## Architecture Decisions

- `INotificationSender` per channel type (`WebhookNotificationSender`, `EmailNotificationSender`); notification send failures are logged to the application log, not the audit log (per [01-REQUIREMENTS.md](01-REQUIREMENTS.md) FR-22).

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| Notification channel add | `CreateNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result<NotificationChannelDto>` | — |
| Notification channel edit | `UpdateNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result<NotificationChannelDto>` | — |
| Notification channel delete | `DeleteNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result` | — |
| Test-send action | `TestNotificationChannelRequestHandler` | `INotificationChannelRepository`, `INotificationDispatcher` | EF repository, `WebhookNotificationSender`/`EmailNotificationSender` | `Result` | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| `/settings` (notifications section) | Paired | `NotificationChannelViewModel`; factory for channel-type-specific field shaping (webhook vs. email fields) | Component owns per-channel form state | N/A |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Email` | Transactional email | `EmailNotificationSender` implementation | Version locked in Phase 01 |
| `SyntaxCircus.Http.Resilience` | Resilient outbound HTTP | Wraps webhook POST calls | Version locked in Phase 01 |

## Deliverables

- [x] Notification channel CRUD in `/settings`.
- [x] `WebhookNotificationSender` (templated JSON body, configurable HTTP method).
- [x] `EmailNotificationSender` (SMTP, per-channel config).
- [x] Fan-out wired into `RunScheduledDomainCheckHandler`/`RunManualDomainCheckRequestHandler` (Phase 06) on `IpChanged`/`UpdateFailed`/`UpdateSucceeded`, filtered by each channel's trigger flags.
- [x] Test-send action per channel.

## Actionable Tasks

- [x] **P08-01** Implement notification channel CRUD handlers and the `/settings` notifications section.
  - **Depends on:** Phase 02 (auth)
  - **Validation:** Handler tests cover validation and not-found branches.
- [x] **P08-02** Implement `WebhookNotificationSender` with template placeholder substitution.
  - **Depends on:** P08-01
  - **Validation:** Unit test confirms placeholders (`{domain}`, `{oldIp}`, `{newIp}`, `{status}`, `{message}`) substitute correctly.
- [x] **P08-03** Implement `EmailNotificationSender` via `SyntaxCircus.Email`.
  - **Depends on:** P08-01
  - **Validation:** Unit test with a substituted SMTP client/sender.
- [x] **P08-04** Wire `INotificationDispatcher` fan-out into the Phase 06 handler, filtered by trigger flags; log send failures to the app log only.
  - **Depends on:** P08-02, P08-03, Phase 06
  - **Validation:** Integration test confirms a failed webhook send does not write to the audit log, only the app log.
- [x] **P08-05** Implement test-send action.
  - **Depends on:** P08-02, P08-03
  - **Validation:** Manual verification — test-send shows success/failure inline. **Not yet performed** (see Success Criteria note below).

## Success Criteria

- [x] `dotnet test` passes for all notification handler/sender tests. (205/205 across all four test projects, including a dedicated integration test proving FR-22's audit-log isolation.)
- [ ] Manual verification: configure a webhook channel, trigger a simulated IP change, confirm the webhook fires with correctly substituted content. **Not performed.** The implementation session's sandbox could not run `dotnet run --project src\StillHere.Web` to completion (the SASS/npm toolchain hung indefinitely, an environment-specific limitation — see `.superpowers/sdd/plan-phase-08-dazzling-lerdorf/progress.md` for details); two implementer subagents *did* successfully start the app and curl-verify `/settings` loads with no DI errors, but no one performed the full interactive click-through (add/edit/delete both channel types, password change, test-send success/failure, live webhook delivery). **A human with a working browser/dev environment should do this pass before considering the phase fully done.**

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

- [x] None specific to this phase.

## Handoff

Phase 09 (Polish) wraps up remaining config (retention, resilience tuning) touching both this phase and Phase 07's data. Next: [PHASE-09-polish.md](PHASE-09-polish.md).
