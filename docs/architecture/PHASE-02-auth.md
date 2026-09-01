# PHASE-02: Auth

## Objective

Implement single-admin authentication: first-run `/setup`, `/login`, cookie auth, `[Authorize]` wiring, and password change.

## Dependencies

- **Depends on:** Phase 01 (solution structure, `AdminUser` entity/migration).
- **Unblocks:** Phase 04 (domain management UI needs `[Authorize]`-protected routes and `ICurrentUserService`).
- **External prerequisites:** None.

## Architecture Decisions

- Custom single-admin cookie auth instead of OAuth/Authentik — see [04-DECISION-LOG.md](04-DECISION-LOG.md) #2.
- Final password hasher choice (`PasswordHasher<T>` vs BCrypt.Net) resolved in this phase — see open question in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions).

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| `/setup` submit | `CreateInitialAdminRequestHandler` | `IAdminUserRepository`, `IPasswordHasher` | EF repository | `Result<AdminUserDto>` → redirect to `/` or show error | — |
| `/login` submit | `AuthenticateAdminRequestHandler` | `IAdminUserRepository`, `IPasswordHasher`, `ICurrentUserService` | EF repository, ASP.NET cookie sign-in | `Result<AuthenticatedAdminDto>` → sets auth cookie or shows validation error | — |
| Change password (`/settings`, auth portion only) | `ChangeAdminPasswordRequestHandler` | `IAdminUserRepository`, `IPasswordHasher`, `ICurrentUserService` | EF repository | `Result` | — |

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

- [ ] `/setup` page, reachable only when no `AdminUser` exists.
- [ ] `/login` page and cookie authentication middleware.
- [ ] `[Authorize]` applied to every route except `/setup` and `/login`.
- [ ] `ICurrentUserService` implementation backed by the auth cookie/claims.
- [ ] Password-change handler wired into `/settings` (page itself may be a stub until Phase 07).

## Actionable Tasks

- [ ] **P02-01** Implement `CreateInitialAdminRequestHandler` and `/setup` page; redirect to `/setup` when no admin exists.
  - **Depends on:** Phase 01
  - **Validation:** Handler test covers no-admin and admin-exists branches; `/setup` unreachable once an admin exists.
- [ ] **P02-02** Implement `AuthenticateAdminRequestHandler`, cookie sign-in, and `/login` page.
  - **Depends on:** P02-01
  - **Validation:** Handler test covers valid/invalid credentials; entry-point test covers cookie sign-in delegation.
- [ ] **P02-03** Wire `[Authorize]` globally except `/setup`/`/login`; implement `ICurrentUserService`.
  - **Depends on:** P02-02
  - **Validation:** Unauthenticated request to any other route redirects to `/login`.
- [ ] **P02-04** Implement `ChangeAdminPasswordRequestHandler`.
  - **Depends on:** P02-02
  - **Validation:** Handler test covers wrong-current-password branch.

## Success Criteria

- [ ] `dotnet test` passes for all handler and entry-point tests in this phase.
- [ ] Manual verification: fresh container forces `/setup`, then `/login` works, then all other routes require auth.

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

- [ ] Final password hasher choice (`PasswordHasher<T>` vs BCrypt.Net) — resolve during P02-01/P02-02 implementation.

## Handoff

Phase 03 and Phase 05 can both start once Phase 01 is done (neither depends on auth); Phase 04 needs this phase complete for `[Authorize]`-protected domain-management routes. Next: [PHASE-03-dns-provider-abstraction.md](PHASE-03-dns-provider-abstraction.md).
