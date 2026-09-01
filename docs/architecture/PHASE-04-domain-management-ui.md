# PHASE-04: Domain Management UI

## Objective

Implement domain CRUD (`/domains/add`, `/domains/{id}/edit`, delete), with provider-driven dynamic credential fields and credential encryption at rest.

## Dependencies

- **Depends on:** Phase 02 (auth/`[Authorize]`), Phase 03 (`IDnsProvider`/`IDnsProviderRegistry`).
- **Unblocks:** Phase 06 (scheduler needs `ManagedDomain` rows to check), Phase 07 (dashboard lists domains).
- **External prerequisites:** None.

## Architecture Decisions

- Provider secrets are encrypted via `ICredentialProtector`, implemented with `Microsoft.AspNetCore.DataProtection` (`IDataProtectionProvider.CreateProtector("StillHere.DnsProviderCredentials")`), not `SyntaxCircus.Credentials` — that package is a desktop OS credential vault, not a server-side encryption library. See [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md) and [04-DECISION-LOG.md](04-DECISION-LOG.md) #4.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| `/domains/add` submit | `AddManagedDomainRequestHandler` | `IManagedDomainRepository`, `IDnsProviderRegistry`, `ICredentialProtector` | EF repository, `Microsoft.AspNetCore.DataProtection` | `Result<ManagedDomainDto>` | — |
| `/domains/{id}/edit` submit | `UpdateManagedDomainRequestHandler` | `IManagedDomainRepository`, `IDnsProviderRegistry`, `ICredentialProtector` | EF repository, `Microsoft.AspNetCore.DataProtection` | `Result<ManagedDomainDto>` | — |
| Domain delete action | `DeleteManagedDomainRequestHandler` | `IManagedDomainRepository` | EF repository | `Result` | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| `/domains/add`, `/domains/{id}/edit` | Paired | `ManagedDomainFormViewModel`; dynamic credential-field list resolved inline in code-behind from `IDnsProviderRegistry.GetByKey(...).CredentialFields` — simple enough to fall under the "factory only for non-trivial mapping" threshold, so no separate factory class | Component owns form state, dynamic field list | N/A |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `Microsoft.AspNetCore.DataProtection` | Server-side secret encryption | Encrypts `DnsProviderCredential.EncryptedSecrets` at rest via `ICredentialProtector`. `SyntaxCircus.Credentials` was originally planned here but excluded — see [04-DECISION-LOG.md](04-DECISION-LOG.md) #4 | Ships with the ASP.NET Core shared framework already targeted by `StillHere.Web` |

## Deliverables

- [x] `/domains/add` and `/domains/{id}/edit` pages with provider-driven dynamic credential fields.
- [x] Domain delete action (with confirmation, per UX brief).
- [x] Credential encryption/decryption wired through `ICredentialProtector`/`Microsoft.AspNetCore.DataProtection`.

## Actionable Tasks

- [x] **P04-01** Implement `AddManagedDomainRequestHandler` and `UpdateManagedDomainRequestHandler`.
  - **Depends on:** Phase 02, Phase 03
  - **Validation:** Handler tests cover validation and provider-field mismatch branches, substituted repo/registry/protector.
- [x] **P04-02** Implement `DeleteManagedDomainRequestHandler`.
  - **Depends on:** P04-01
  - **Validation:** Handler test covers not-found branch.
- [x] **P04-03** Build `/domains/add`/`/domains/{id}/edit` paired components with dynamic field resolution.
  - **Depends on:** P04-01
  - **Validation:** Manual verification — selecting Namecheap renders its credential fields.
- [x] **P04-04** Wire `ICredentialProtector` (`Microsoft.AspNetCore.DataProtection`) into add/edit handlers.
  - **Depends on:** P04-01
  - **Validation:** Integration test confirms secrets are stored encrypted, not plaintext, in SQLite.

## Success Criteria

- [x] `dotnet test` passes for all domain-management handler tests.
- [x] Manual verification: add, edit, and delete a domain end-to-end through the UI; confirm secrets are encrypted at rest.

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
- [x] Duplicated-looking logic across flows was evaluated for genuine divergence before extracting (or intentionally not extracting) a shared abstraction (`CredentialFieldValidator` extracted, shared by add/update handlers).

## Risks and Open Questions

- [x] None specific to this phase.

## Handoff

Phase 06 (Scheduler) and Phase 07 (Dashboard) both need `ManagedDomain` CRUD to exist first. Next: [PHASE-05-ip-detection.md](PHASE-05-ip-detection.md) (can run in parallel with this phase — see [99-IMPLEMENTATION-ROADMAP.md](99-IMPLEMENTATION-ROADMAP.md)).
