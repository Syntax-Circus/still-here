# PHASE-03: DNS Provider Abstraction

## Objective

Implement the `IDnsProvider` abstraction and the v1 Namecheap provider, with unit tests against a mocked HTTP handler.

## Dependencies

- **Depends on:** Phase 01 (solution structure, `DnsProviderCredential`/`ManagedDomain` entities).
- **Unblocks:** Phase 04 (domain-add UI needs `IDnsProviderRegistry.CredentialFields` for dynamic forms), Phase 06 (scheduler calls `IDnsProvider.UpdateAsync`).
- **External prerequisites:** Namecheap DDNS API docs (verify current XML response schema — see open question).

## Architecture Decisions

- `IDnsProvider` is registered via DI as `IEnumerable<IDnsProvider>` and resolved by `ProviderKey`, so adding `CloudflareDnsProvider` later requires no changes to scheduler, UI list rendering, or audit logging (per [02-ARCHITECTURE.md](02-ARCHITECTURE.md)).
- Established the convention for non-repository application-facing abstractions (this is the first one, alongside Phase 02's `IAdminUserRepository`/`IAdminPasswordHasher`): contracts + DTOs (`IDnsProvider`, `IDnsProviderRegistry`, `ProviderCredentialField`, `DnsUpdateRequest`, `DnsUpdateResult`) live in `StillHere.Application/Features/DnsProviders/`; concrete implementations (`NamecheapDnsProvider`) live in `StillHere.Infrastructure/DnsProviders/`. `IDnsProviderRegistry`'s implementation has no infrastructure dependency (pure aggregation over DI-injected `IEnumerable<IDnsProvider>`), so it lives directly in Application.
- `DnsUpdateResult` is a plain service-level record, not `SyntaxCircus.Common.Result<T>` — it isn't a named-handler outcome mapped to transport; Phase 06's handlers translate it into their own `Result<T>` at their own boundary, the same way repositories return plain DTOs rather than `Result<T>`.
- Verified Namecheap's real DDNS contract directly (their own docs plus two independent real-world reports) rather than trusting the original informal plan's assumptions. Two gotchas shaped the implementation:
  1. The XML response always declares `encoding="utf-16"` while the actual bytes are UTF-8 (a known Namecheap bug). `NamecheapDnsProvider` reads the raw bytes, decodes explicitly as UTF-8, and parses the resulting *string* — parsing a string sidesteps the false declaration entirely, since a string has no byte-level encoding left to misdetect.
  2. HTTP status is always 200, even on Namecheap-side errors — success/failure comes from the XML body (`ErrCount`/`Done`), never the transport status code. This is exactly why `SyntaxCircus.Http.Resilience`'s retry/circuit-breaker (which only trigger on exceptions or HTTP ≥500/429) is the right fit: genuine transient network failures get retried automatically, while Namecheap's own permanent errors (bad password, domain not found) pass through as a single attempt, which is correct — retrying those would be pointless.

## Application Boundaries

Follow [Application Architecture](../APPLICATION_ARCHITECTURE.md). `IDnsProvider` implementations are infrastructure, not entry points — no new entry point is added by this phase; they are invoked by handlers introduced in Phase 04 and Phase 06.

| Entry point/use case | Named handler | Allowed abstractions | Infrastructure implementation | Outcome/transport mapping | Decision |
| :------------------- | :------------ | :------------------- | :----------------------------- | :------------------------ | :------- |
| — (infrastructure-only phase) | — | — | `NamecheapDnsProvider : IDnsProvider`, registered via `SyntaxCircus.Http.Resilience`'s `AddResilientHttpClient(...).AddTypedClient<IDnsProvider, NamecheapDnsProvider>()` | — | — |

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

- [x] `IDnsProvider` interface (`ProviderKey`, `DisplayName`, `CredentialFields`, `UpdateAsync`).
- [x] `IDnsProviderRegistry`/`DnsProviderRegistry` (`Providers`, `GetByKey`) — referenced by name elsewhere in this doc and in `02-ARCHITECTURE.md`, but not itemized as its own deliverable in the original version of this doc; added here since Phase 04 depends on it directly.
- [x] `NamecheapDnsProvider` implementation, DI-registered.
- [x] Unit tests mocking `HttpMessageHandler`, covering success and Namecheap XML error cases (plus the always-200, missing-credential, malformed-response, network-failure, and false-utf16-declaration cases — see `NamecheapDnsProviderTests.cs`).

## Actionable Tasks

- [x] **P03-01** Define `IDnsProvider` and `ProviderCredentialField` contracts.
  - **Depends on:** Phase 01
  - **Validation:** Compiles; no infrastructure types leak into the interface.
- [x] **P03-02** Verify Namecheap's current DDNS response XML schema against its docs.
  - **Depends on:** —
  - **Validation:** Resolves the open question in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions). Verified schema and the two gotchas are documented above under Architecture Decisions.
- [x] **P03-03** Implement `NamecheapDnsProvider.UpdateAsync` wrapped in `SyntaxCircus.Http.Resilience` retry policy.
  - **Depends on:** P03-01, P03-02
  - **Validation:** Unit tests cover success, provider-reported-IP extraction, and XML error parsing against a mocked handler.
- [x] **P03-04** Register `IDnsProvider` implementations via DI as `IEnumerable<IDnsProvider>`, resolvable by `ProviderKey`.
  - **Depends on:** P03-03
  - **Validation:** DI resolution test confirms `NamecheapDnsProvider` is discoverable by its `ProviderKey` (`DnsProviderDependencyInjectionTests.cs`).

## Success Criteria

- [x] `dotnet test` passes for all `NamecheapDnsProvider` unit tests, including at least one XML error case (41/41 solution-wide).
- [x] Adding a hypothetical second provider requires no change outside a new class + DI registration (verified by code review against [02-ARCHITECTURE.md](02-ARCHITECTURE.md)'s stated goal, and concretely by `DnsProviderDependencyInjectionTests.cs`).

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

- [x] Exact Namecheap DDNS response XML fields to parse — resolved in P03-02 (see Architecture Decisions above).
- [ ] Namecheap may change its response schema without notice (ongoing risk, not just an at-implementation-time question — stays open indefinitely).

## Handoff

Phase 04 needs `IDnsProvider.CredentialFields` to drive dynamic domain-add forms; Phase 06 needs `IDnsProvider.UpdateAsync`. Next: [PHASE-04-domain-management-ui.md](PHASE-04-domain-management-ui.md).
