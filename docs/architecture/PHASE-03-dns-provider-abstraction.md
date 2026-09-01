# PHASE-03: DNS Provider Abstraction

## Objective

Implement the `IDnsProvider` abstraction and the v1 Namecheap provider, with unit tests against a mocked HTTP handler.

## Dependencies

- **Depends on:** Phase 01 (solution structure, `DnsProviderCredential`/`ManagedDomain` entities).
- **Unblocks:** Phase 04 (domain-add UI needs `IDnsProviderRegistry.CredentialFields` for dynamic forms), Phase 06 (scheduler calls `IDnsProvider.UpdateAsync`).
- **External prerequisites:** Namecheap DDNS API docs (verify current XML response schema — see open question).

## Architecture Decisions

- `IDnsProvider` is registered via DI as `IEnumerable<IDnsProvider>` and resolved by `ProviderKey`, so adding `CloudflareDnsProvider` later requires no changes to scheduler, UI list rendering, or audit logging (per [02-ARCHITECTURE.md](02-ARCHITECTURE.md)).

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md). `IDnsProvider` implementations are infrastructure, not entry points — no new entry point is added by this phase; they are invoked by handlers introduced in Phase 04 and Phase 06.

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| — (infrastructure-only phase) | — | — | `NamecheapDnsProvider : IDnsProvider` | — | — |

## Razor Component Boundaries

Follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md). No Razor components in this phase.

| Component/feature | `.razor.cs` decision | ViewModel/factory decision | State behavior | API DTO boundary |
| :----------------- | :-------------------- | :--------------------------- | :-------------- | :----------------- |
| — | — | — | — | — |

## Syntax Circus Packages

| Package | Concern | Why it belongs in this phase | Verification |
| :------ | :------ | :---------------------------- | :------------- |
| `SyntaxCircus.Http.Resilience` | Resilient outbound HTTP | Wraps the Namecheap DDNS GET call with retry/backoff | Version locked in Phase 01 |

## Deliverables

- [ ] `IDnsProvider` interface (`ProviderKey`, `DisplayName`, `CredentialFields`, `UpdateAsync`).
- [ ] `NamecheapDnsProvider` implementation, DI-registered.
- [ ] Unit tests mocking `HttpMessageHandler`, covering success and Namecheap XML error cases.

## Actionable Tasks

- [ ] **P03-01** Define `IDnsProvider` and `ProviderCredentialField` contracts.
  - **Depends on:** Phase 01
  - **Validation:** Compiles; no infrastructure types leak into the interface.
- [ ] **P03-02** Verify Namecheap's current DDNS response XML schema against its docs.
  - **Depends on:** —
  - **Validation:** Resolves the open question in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions).
- [ ] **P03-03** Implement `NamecheapDnsProvider.UpdateAsync` wrapped in `SyntaxCircus.Http.Resilience` retry policy.
  - **Depends on:** P03-01, P03-02
  - **Validation:** Unit tests cover success, provider-reported-IP extraction, and XML error parsing against a mocked handler.
- [ ] **P03-04** Register `IDnsProvider` implementations via DI as `IEnumerable<IDnsProvider>`, resolvable by `ProviderKey`.
  - **Depends on:** P03-03
  - **Validation:** DI resolution test confirms `NamecheapDnsProvider` is discoverable by its `ProviderKey`.

## Success Criteria

- [ ] `dotnet test` passes for all `NamecheapDnsProvider` unit tests, including at least one XML error case.
- [ ] Adding a hypothetical second provider requires no change outside a new class + DI registration (verified by code review against [02-ARCHITECTURE.md](02-ARCHITECTURE.md)'s stated goal).

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

- [ ] Exact Namecheap DDNS response XML fields to parse — resolved in P03-02.
- [ ] Namecheap may change its response schema without notice (ongoing risk, not just an at-implementation-time question).

## Handoff

Phase 04 needs `IDnsProvider.CredentialFields` to drive dynamic domain-add forms; Phase 06 needs `IDnsProvider.UpdateAsync`. Next: [PHASE-04-domain-management-ui.md](PHASE-04-domain-management-ui.md).
