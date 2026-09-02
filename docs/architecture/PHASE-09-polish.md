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

- [x] Finalized Serilog configuration (console + rolling file, per [02-ARCHITECTURE.md](02-ARCHITECTURE.md) Docker section).
- [x] Tuned retry/backoff policies for provider, IP-check, and notification HTTP calls.
- [x] Audit log retention/pruning job honoring `GlobalSettings.AuditLogRetentionDays`.
- [x] Repo root `README.md` with docker-compose usage instructions.

## Actionable Tasks

- [x] **P09-01** Finalize Serilog sinks/enrichers per `SyntaxCircus.AspNetCore.Serilog` conventions.
  - **Depends on:** Phase 01
  - **Validation:** Manual verification — `docker logs` shows structured output; rolling file appears in `/data/logs`. **Done, with a fix beyond the original ask:** the pre-existing `Logging:LogLevel` overrides (including this phase's own `Microsoft.EntityFrameworkCore` addition) were silently inert — `AddStandardSerilog` calls `ReadFrom.Configuration(builder.Configuration)`, which reads Serilog's own `Serilog:MinimumLevel`/`Serilog:MinimumLevel:Override` schema, not ASP.NET Core's `Logging:LogLevel` section. Moved the overrides to `Serilog:MinimumLevel:Override` in both `appsettings.json` and `appsettings.Development.json`; verified via a live `docker compose up` that noisy `Executing endpoint`/`Executed DbCommand` lines disappear from both `docker logs` and the rolling file, confirmed against the actual `SyntaxCircus.AspNetCore.Serilog` package source.
- [x] **P09-02** Review and tune `SyntaxCircus.Http.Resilience` policies across Namecheap, IP-check, and webhook calls for consistent retry counts/backoff.
  - **Depends on:** Phase 03, Phase 05, Phase 08
  - **Validation:** Named constants for retry counts/backoff, not bare magic numbers (per [../APPLICATION_ARCHITECTURE.md](../APPLICATION_ARCHITECTURE.md) Constants Over Magic Values). `AddResilientHttpClient` exposes no separate backoff/delay parameter (exponential backoff + jitter is built in), so retry count and timeout are the only tunables — both named per client (`NamecheapDnsProvider.MaxRetryAttempts`/`.HttpTimeout`, `IpDetectionService.MaxRetryAttempts`/`.HttpTimeout`, `WebhookNotificationSender.MaxRetryAttempts`/`.HttpTimeout`), values kept differentiated per client's risk profile rather than forced identical.
- [x] **P09-03** Implement `PruneExpiredAuditLogEntriesHandler` and its scheduled trigger.
  - **Depends on:** Phase 07
  - **Validation:** Unit test confirms entries older than `AuditLogRetentionDays` are removed and `null` retention keeps everything. Implemented as `IAuditLogRepository.PruneExpiredAsync` (the only abstraction this handler is allowed per the table below) doing the retention-window read and bulk delete together in one EF operation; `PruneExpiredAuditLogEntriesHandler` is a thin pass-through; `AuditLogRetentionScheduler` (daily tick, mirrors `DomainCheckScheduler`) invokes it. TDD throughout — repository, handler, and scheduler tests all written and watched red before implementation.
- [x] **P09-04** Write repo root `README.md`: what still-here is, docker-compose usage, first-run instructions.
  - **Depends on:** All prior phases
  - **Validation:** Manual verification — following the README from a clean checkout gets a running container. Verified live via `docker compose up -d --build` against this repo's Dockerfile/docker-compose.yml: image builds, container starts, `/healthz` and `/login` return 200, structured logs appear in both `docker logs` and the mounted volume's `logs/` directory. Ran against this machine's existing `still-here_stillhere-data` dev volume (already has an admin account from earlier phases' manual verification), so it redirected straight to `/login` rather than `/setup` — the `/setup` first-run path itself is unchanged by this phase and already covered by Phase 02's own tests.

## Success Criteria

- [x] `dotnet test` passes for the full suite across all phases. (210/210 across all four test projects.)
- [x] `docker compose up` from a clean checkout, following the new root README, produces a working app reachable at `/`. (Verified live — see P09-04 note above on the one caveat: tested against a pre-existing dev volume, not a literally empty one.)
- [x] Retention pruning verified to remove only entries older than the configured window. (Automated tests plus observed live: a real tick logged `Pruned 0 expired audit log entries.` against the current dev database.)

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

This is the final v1 phase. Any post-v1 work (Cloudflare provider, multi-user, IPv6) starts a new discovery cycle per `_template/docs/ARCHITECTURE_DISCOVERY.md`, not an ad-hoc addition to this phase set.
