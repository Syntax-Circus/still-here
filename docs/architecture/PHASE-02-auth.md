# PHASE-02: Auth

## Objective

Implement single-admin authentication: first-run `/setup`, `/login`, cookie auth, `[Authorize]` wiring, and password change.

## Dependencies

- **Depends on:** Phase 01 (solution structure, `AdminUser` entity/migration).
- **Unblocks:** Phase 04 (domain management UI needs `[Authorize]`-protected routes and `ICurrentUserService`).
- **External prerequisites:** None.

## Architecture Decisions

- Custom single-admin cookie auth instead of OAuth/Authentik — see [04-DECISION-LOG.md](04-DECISION-LOG.md) #2.
- Password hasher: `Microsoft.AspNetCore.Identity.PasswordHasher<T>`, used standalone via `PasswordHasher<string>` behind a project-local `IAdminPasswordHasher` — not BCrypt.Net, not full ASP.NET Core Identity. Matches the real `ApiKeyHasher` precedent in sibling repo `cryp-tradr`. Resolves the open question from [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions).
- Sign-in mechanism: plain `<form method="post">` submissions to `MapPost` minimal-API entry points (`AuthEndpoints.cs`), not interactive `@onclick` handlers — Blazor Server's SignalR circuits can't set response cookies mid-render. Matches the real, working pattern in sibling repo `cmsify.Admin`.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| `/setup` submit | `CreateInitialAdminRequestHandler` | `IAdminUserRepository`, `IAdminPasswordHasher` | EF repository | `Result<AdminUserDto>` → redirect to `/` or show error | — |
| `/login` submit | `AuthenticateAdminRequestHandler` | `IAdminUserRepository`, `IAdminPasswordHasher` | EF repository. Cookie sign-in is entry-point-owned (`AuthEndpoints.cs`), not a handler dependency | `Result<AuthenticatedAdminDto>` → entry point sets auth cookie on success or shows validation error | — |
| Change password (handler only — see Deliverables note on `/settings`) | `ChangeAdminPasswordRequestHandler` | `IAdminUserRepository`, `IAdminPasswordHasher`, `ICurrentUserService` | EF repository | `Result` | — |
| `/setup`, `/login` first-run routing | `FirstRunGateMiddleware` (`UseFirstRunGate`) | `IAdminUserRepository` | — | Redirects; runs before `UseAuthentication`/`UseAuthorization` so a fresh install reaches `/setup` before the cookie scheme's own `LoginPath` challenge would otherwise intercept it | Exempt — pure routing decision, no `Result` semantics, same category as `/healthz`'s inline `Database.CanConnectAsync()` check |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| `/setup` | Paired | `SetupViewModel` (feature-local) | Component owns form state | N/A |
| `/login` | Paired | `LoginViewModel` (feature-local) | Component owns form/validation state | N/A |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Common` | `Result`/`ICurrentUserService` | Auth handlers return `Result`; `ICurrentUserService` backs identity for every later phase | Version locked in Phase 01 |

## Deliverables

- [x] `/setup` page, reachable only when no `AdminUser` exists.
- [x] `/login` page and cookie authentication middleware.
- [x] `[Authorize]` applied to every route except `/setup` and `/login`.
- [x] `ICurrentUserService` — already provided by `SyntaxCircus.Common`'s `AddCurrentUserService()` (Phase 01); reads the auth cookie's claims via `IHttpContextAccessor`, no project-specific implementation needed.
- [x] `ChangeAdminPasswordRequestHandler` implemented and unit-tested. Not wired into a `/settings` page yet — `/settings` doesn't structurally exist until Phase 08 (notifications) per the phase breakdown, so there is nothing to wire it into yet. This phase's own wording ("page itself may be a stub until Phase 07") was aspirational/imprecise; Phase 08 is the correct owner.

## Actionable Tasks

- [x] **P02-01** Implement `CreateInitialAdminRequestHandler` and `/setup` page; redirect to `/setup` when no admin exists.
  - **Depends on:** Phase 01
  - **Validation:** Handler test covers no-admin and admin-exists branches; `/setup` unreachable once an admin exists.
- [x] **P02-02** Implement `AuthenticateAdminRequestHandler`, cookie sign-in, and `/login` page.
  - **Depends on:** P02-01
  - **Validation:** Handler test covers valid/invalid credentials; entry-point test covers cookie sign-in delegation.
- [x] **P02-03** Wire `[Authorize]` globally except `/setup`/`/login`; implement `ICurrentUserService`.
  - **Depends on:** P02-02
  - **Validation:** Unauthenticated request to any other route redirects to `/login`.
- [x] **P02-04** Implement `ChangeAdminPasswordRequestHandler`.
  - **Depends on:** P02-02
  - **Validation:** Handler test covers wrong-current-password branch.

## Success Criteria

- [x] `dotnet test` passes for all handler and entry-point tests in this phase (30/30 solution-wide).
- [x] Manual verification (via curl + cookie jar) and the automated `StillHere.Web.Tests` suite: fresh install forces `/setup`, `/setup` becomes unreachable once an admin exists, `/login` works (valid and invalid credentials), all other routes require auth, `/logout` clears the session, `/healthz` unaffected.

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

- [x] Final password hasher choice — resolved: `PasswordHasher<T>`, standalone (see Architecture Decisions above).

## Handoff

Phase 03 and Phase 05 can both start once Phase 01 is done (neither depends on auth); Phase 04 needs this phase complete for `[Authorize]`-protected domain-management routes. Next: [PHASE-03-dns-provider-abstraction.md](PHASE-03-dns-provider-abstraction.md).
