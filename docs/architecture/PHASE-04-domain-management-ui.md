# PHASE-04: Domain Management UI

## Objective

Implement domain CRUD (`/domains/add`, `/domains/{id}/edit`, delete), with provider-driven dynamic credential fields and credential encryption at rest.

## Dependencies

- **Depends on:** Phase 02 (auth/`[Authorize]`), Phase 03 (`IDnsProvider`/`IDnsProviderRegistry`).
- **Unblocks:** Phase 06 (scheduler needs `ManagedDomain` rows to check), Phase 07 (dashboard lists domains).
- **External prerequisites:** None.

## Architecture Decisions

- Provider secrets are encrypted via `SyntaxCircus.Credentials` (`ICredentialProtector`), not raw ASP.NET Data Protection API directly — see [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md).

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md).

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| `/domains/add` submit | `AddManagedDomainRequestHandler` | `IManagedDomainRepository`, `IDnsProviderRegistry`, `ICredentialProtector` | EF repository, `SyntaxCircus.Credentials` | `Result<ManagedDomainDto>` | — |
| `/domains/{id}/edit` submit | `UpdateManagedDomainRequestHandler` | `IManagedDomainRepository`, `ICredentialProtector` | EF repository, `SyntaxCircus.Credentials` | `Result<ManagedDomainDto>` | — |
| Domain delete action | `DeleteManagedDomainRequestHandler` | `IManagedDomainRepository` | EF repository | `Result` | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md).

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| `/domains/add`, `/domains/{id}/edit` | Paired | `ManagedDomainFormViewModel`; factory builds the dynamic credential-field list from `IDnsProviderRegistry` | Component owns form state, dynamic field list | N/A |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Credentials` | Local credential storage | Encrypts `DnsProviderCredential.EncryptedSecrets` at rest | Version locked in Phase 01 |

## Deliverables

- [ ] `/domains/add` and `/domains/{id}/edit` pages with provider-driven dynamic credential fields.
- [ ] Domain delete action (with confirmation, per UX brief).
- [ ] Credential encryption/decryption wired through `SyntaxCircus.Credentials`.

## Actionable Tasks

- [ ] **P04-01** Implement `AddManagedDomainRequestHandler` and `UpdateManagedDomainRequestHandler`.
  - **Depends on:** Phase 02, Phase 03
  - **Validation:** Handler tests cover validation and provider-field mismatch branches, substituted repo/registry/protector.
- [ ] **P04-02** Implement `DeleteManagedDomainRequestHandler`.
  - **Depends on:** P04-01
  - **Validation:** Handler test covers not-found branch.
- [ ] **P04-03** Build `/domains/add`/`/domains/{id}/edit` paired components with dynamic field factory.
  - **Depends on:** P04-01
  - **Validation:** Manual verification — selecting Namecheap renders its credential fields.
- [ ] **P04-04** Wire `ICredentialProtector` (`SyntaxCircus.Credentials`) into add/edit handlers.
  - **Depends on:** P04-01
  - **Validation:** Integration test confirms secrets are stored encrypted, not plaintext, in SQLite.

## Success Criteria

- [ ] `dotnet test` passes for all domain-management handler tests.
- [ ] Manual verification: add, edit, and delete a domain end-to-end through the UI; confirm secrets are encrypted at rest.

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

Phase 06 (Scheduler) and Phase 07 (Dashboard) both need `ManagedDomain` CRUD to exist first. Next: [PHASE-05-ip-detection.md](PHASE-05-ip-detection.md) (can run in parallel with this phase — see [99-IMPLEMENTATION-ROADMAP.md](99-IMPLEMENTATION-ROADMAP.md)).
