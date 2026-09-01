# PHASE-01: Foundation

## Objective

Stand up the solution skeleton, EF Core data model with initial SQLite migration, Dockerfile/compose skeleton, and the `/healthz` endpoint — no business features yet.

## Dependencies

- **Depends on:** None (first phase).
- **Unblocks:** All other phases.
- **External prerequisites:** .NET 10 SDK available; Docker available for compose skeleton verification.

## Architecture Decisions

- SQLite over Postgres — see [04-DECISION-LOG.md](04-DECISION-LOG.md) #1.
- `Directory.Packages.props` is created in this phase, locking every package version selected in [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md).

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md). List every server-side entry point added or changed by this phase.

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| `/healthz` | *(exempt)* | — | — | Returns 200 when the app/DB is reachable | Exempt — no application workflow |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). No Razor components are added in this phase (scaffold only).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — | — | — | — | — |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Common` | Result-type handler outcomes | Foundational — used by every handler from Phase 02 onward | Verify latest stable release on GitHub before locking version |
| `SyntaxCircus.AspNetCore.Serilog` | Structured host logging | Foundational logging setup | Verify latest stable release on GitHub before locking version |
| `SyntaxCircus.Blazor.Components` | Error boundary/reconnect UI | Wired into the Blazor host shell from the start | Verify latest stable release on GitHub before locking version |
| `SyntaxCircus.DotEnv` | Dev `.env` loading | Local dev configuration from the start | Verify latest stable release on GitHub before locking version |

## Deliverables

- [ ] Solution/project structure created (host project, application project, infrastructure project, per the mandatory dependency flow).
- [ ] `Directory.Packages.props` with every 03-PACKAGE-MAP.md "Selected" package locked to an exact, verified version.
- [ ] EF Core entities (`AdminUser`, `DnsProviderCredential`, `ManagedDomain`, `AuditLogEntry`, `GlobalSettings`, `NotificationChannel`) and an initial tool-generated migration.
- [ ] Dockerfile (multi-stage) and `docker-compose.yml` skeleton with the `/data` volume mount.
- [ ] `/healthz` endpoint.

## Actionable Tasks

- [ ] **P01-01** Create solution structure separating host, application, and infrastructure projects per the mandatory dependency flow.
  - **Depends on:** —
  - **Validation:** `dotnet build` succeeds; infrastructure project references application contracts, not vice versa.
- [ ] **P01-02** Verify and lock exact versions for every 03-PACKAGE-MAP.md "Selected" and non-SyntaxCircus package in `Directory.Packages.props`.
  - **Depends on:** P01-01
  - **Validation:** Every package restores at the pinned version; 03-PACKAGE-MAP.md updated with the verified version and source link.
- [ ] **P01-03** Define EF Core entities and generate the initial migration via `dotnet ef migrations add`.
  - **Depends on:** P01-01
  - **Validation:** `dotnet ef database update` against a local SQLite file succeeds; entities match [02-ARCHITECTURE.md § Data Model](02-ARCHITECTURE.md#data-model-ef-core-entities).
- [ ] **P01-04** Add multi-stage Dockerfile and docker-compose skeleton with `/data` volume.
  - **Depends on:** P01-01
  - **Validation:** `docker compose build` succeeds; container starts and serves `/healthz`.
- [ ] **P01-05** Add `/healthz` endpoint reporting DB reachability.
  - **Depends on:** P01-03
  - **Validation:** `GET /healthz` returns 200 with the DB reachable, non-200 otherwise.

## Success Criteria

- [ ] `dotnet build` and `dotnet test` (empty test suite is acceptable at this phase) succeed.
- [ ] `docker compose up` starts the container and `/healthz` responds 200.
- [ ] Every package in `Directory.Packages.props` has an exact version and a verified source link recorded in `03-PACKAGE-MAP.md`.

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

- [ ] None specific to this phase beyond the general open questions in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions).

## Handoff

Phase 02 (Auth) can start once the solution builds, the initial migration applies cleanly, and `Directory.Packages.props` is locked. Next: [PHASE-02-auth.md](PHASE-02-auth.md).
