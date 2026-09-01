# 04 - Decision Log: still-here

## Decision 1: SQLite Instead of Postgres

- **Status:** Accepted
- **Date:** 2026-09-01
- **Owner:** Project owner
- **Related artifacts:** [PROJECT_BRIEF.md](../PROJECT_BRIEF.md), [02-ARCHITECTURE.md](02-ARCHITECTURE.md), [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md)

### Context

The template's sample stack defaults to Postgres via `SyntaxCircus.EntityFrameworkCore.Postgres`. still-here is a single-container, single-admin, home-hosted tool with a small, low-write dataset (~12 domains).

### Decision

Use EF Core with the SQLite provider, persisted to a file on the container's mounted `/data` volume. Do not stand up or depend on a separate Postgres service.

### Boundary Deviation Details

Not a boundary-pattern deviation (entry-point/handler layering is unaffected) — this is a technology-preference deviation from the template's *sample* stack, not from a mandatory rule. Recorded here per the template's convention of logging material technology decisions.

- **Violated rule:** None (`sample_prompt.md` stack preference only, not a mandatory rule).
- **Exact affected scope:** Persistence layer only (`Microsoft.EntityFrameworkCore.Sqlite` instead of `SyntaxCircus.EntityFrameworkCore.Postgres`).
- **Consequences:** No separate DB container/service to operate; simpler single-container deployment; no built-in horizontal scaling or multi-instance write concurrency (acceptable — single admin, single instance).
- **Approval:** Project owner, 2026-09-01.
- **Disposition:** Permanent for v1; revisit only if multi-instance/HA becomes a requirement.

### Alternatives Considered

- Postgres (template default) — rejected: adds a second container/managed dependency with no benefit at this scale.

### Consequences

- Positive: single-container simplicity matches the stated deployment goal.
- Negative: no `SyntaxCircus.EntityFrameworkCore.Postgres` reuse; SQLite-specific EF quirks (e.g. limited concurrent writes) apply, acceptable at this scale.

### Approval

- **Approved by:** Project owner
- **Approved on:** 2026-09-01

---

## Decision 2: Custom Single-Admin Cookie Auth Instead of OAuth/Authentik

- **Status:** Accepted
- **Date:** 2026-09-01
- **Owner:** Project owner
- **Related artifacts:** [PROJECT_BRIEF.md](../PROJECT_BRIEF.md), [02-ARCHITECTURE.md](02-ARCHITECTURE.md), [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md)

### Context

The template's sample stack defaults to OAuth via Authentik. still-here has exactly one user (the owner) and no external identity provider available or needed for a home-hosted personal tool.

### Decision

Use a custom, minimal cookie-auth implementation (single admin account, `PasswordHasher<T>` or BCrypt-hashed password), gated by a first-run `/setup` flow. Keep `AdminUser` as a real table (not an env-var check) to keep a future multi-user/OAuth migration low-effort.

### Boundary Deviation Details

- **Violated rule:** None directly — the `ICurrentUserService` abstraction and handler boundary rules from `APPLICATION_ARCHITECTURE.md` are still followed; this deviates from the template's *sample* auth technology, not its mandatory handler/entry-point rules.
- **Exact affected scope:** Auth implementation only (`SyntaxCircus.Blazor.Auth`/`SyntaxCircus.AspNetCore.Authentication` marked Not applicable in [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md)).
- **Consequences:** No external IdP dependency or token-forwarding complexity; single-admin only until a future migration; password reset/recovery is a manual/self-hosted concern (no IdP-backed recovery flow).
- **Approval:** Project owner, 2026-09-01.
- **Disposition:** Permanent for v1; removal condition = a future multi-user requirement, at which point `AdminUser` migrates to a real identity provider.

### Alternatives Considered

- OAuth/Authentik (template default) — rejected: requires standing up/maintaining an external IdP for a single-user home tool, disproportionate to the need.

### Consequences

- Positive: minimal auth surface, no external dependency, faster to implement and operate.
- Negative: no SSO/MFA out of the box; deferred multi-user support requires a later migration.

### Approval

- **Approved by:** Project owner
- **Approved on:** 2026-09-01

---

## Decision 3: Full Handler-Per-Entry-Point Pattern Despite Small App Size

- **Status:** Accepted
- **Date:** 2026-09-01
- **Owner:** Project owner
- **Related artifacts:** [02-ARCHITECTURE.md](02-ARCHITECTURE.md) (application boundary table), all `PHASE-NN-*.md` files

### Context

still-here is a small, single-admin app where a lighter direct-service-call design (Blazor code-behind calling `IDnsProvider`/`IIpDetectionService`/etc. directly) would reduce ceremony. The template mandates entry point → named handler → service/repo → infra layering for every application use case.

### Decision

Apply the full pattern: every Blazor page action and the scheduler tick delegates to a named use-case handler (see the application boundary table in [02-ARCHITECTURE.md](02-ARCHITECTURE.md)), consistent with `../APPLICATION_ARCHITECTURE.md`.

### Boundary Deviation Details

Not applicable — this decision *adopts* the mandatory pattern rather than deviating from it. Logged here for traceability since it was an explicit, discussed choice rather than a silent default.

### Alternatives Considered

- Lightweight direct-service design — rejected: the project owner explicitly prioritized template consistency (easier movement between Syntax Circus projects, and easier handoff of phase-by-phase work) over minimizing ceremony in this specific small app.

### Consequences

- Positive: consistent with every other Syntax Circus project; handler tests are substitute-based and fast; easy to extend later (e.g. adding Cloudflare as a second provider, or a second admin).
- Negative: more files/interfaces than a minimal direct-call design would need for an app this size.

### Approval

- **Approved by:** Project owner
- **Approved on:** 2026-09-01

---

## Decision 4: ASP.NET Core Data Protection Instead of SyntaxCircus.Credentials

- **Status:** Accepted
- **Date:** 2026-09-01
- **Owner:** Project owner
- **Related artifacts:** [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md), [PHASE-04-domain-management-ui.md](PHASE-04-domain-management-ui.md), [PROJECT_BRIEF.md](../PROJECT_BRIEF.md)

### Context

Earlier planning (`03-PACKAGE-MAP.md`, `PHASE-04-domain-management-ui.md`, `PROJECT_BRIEF.md`) assumed `SyntaxCircus.Credentials` provides an `ICredentialProtector`-style server-side encryption API for encrypting `DnsProviderCredential.EncryptedSecrets` at rest. Reading its actual v0.1.1 source during Phase 04 implementation showed this is wrong: its only interface is `ICredentialStore` (`GetAsync`/`SetAsync`/`DeleteAsync`/`ExistsAsync` keyed by `(serviceId, accountId)`), backed by the Windows Credential Manager, macOS Keychain, or a Linux `secret-tool` D-Bus service — a desktop OS credential vault, not a database-column encryption library. There is no `ICredentialProtector`, no purpose-string API, and no relationship to ASP.NET Core Data Protection. Its own usage guide states it is "not... a server secret store." A headless Docker container (still-here's deployment target) has no OS keychain, and the package's Linux fallback (`EncryptedFileCredentialStore`) is explicitly disclaimed in its own docs as "not a substitute for a hardened OS keychain" and "not a server secret store."

### Decision

Back `ICredentialProtector` (the name every doc already used) with `Microsoft.AspNetCore.DataProtection` instead — the framework's own built-in mechanism for exactly this need, already registered in `Program.cs` (`AddDataProtection().PersistKeysToFileSystem(...)`) for the auth cookie. No new package is needed. `CredentialProtector` wraps `IDataProtectionProvider.CreateProtector("StillHere.DnsProviderCredentials")`, a distinct purpose string giving real cryptographic isolation from future secret categories (e.g. SMTP passwords in Phase 08).

### Boundary Deviation Details

Not a boundary-pattern deviation — `ICredentialProtector` still lives in `StillHere.Application` with its implementation in `StillHere.Infrastructure`, matching the mandatory abstraction/implementation split. This is a package-selection correction, not a rule deviation.

- **Violated rule:** None.
- **Exact affected scope:** `SyntaxCircus.Credentials` moves from Selected to Excluded in [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md); `Microsoft.AspNetCore.DataProtection` is documented as the credential-encryption mechanism instead.
- **Consequences:** No new package dependency (Data Protection already ships with the ASP.NET Core shared framework); one fewer external package to track; encryption keys already persisted to the container's mounted volume via the existing `PersistKeysToFileSystem` configuration.
- **Approval:** Project owner, 2026-09-01 (retroactive — corrects a planning-time misunderstanding discovered during implementation).
- **Disposition:** Permanent; `SyntaxCircus.Credentials` would only become relevant again if still-here grew a desktop-client component.

### Alternatives Considered

- `SyntaxCircus.Credentials` as originally planned — rejected: not applicable to a server-side/headless deployment; would require an OS keychain still-here's Docker container doesn't have.

### Consequences

- Positive: uses a framework mechanism already wired into the app; no new package surface to learn or version; the package map's package-count stays lower.
- Negative: the SyntaxCircus catalog's cross-project consistency benefit (using the same credential package everywhere) doesn't apply here, since the catalog package solves a different problem than the one this app has.

### Approval

- **Approved by:** Project owner
- **Approved on:** 2026-09-01

---

## Decision 5: Split Scoped `IpDetectionService` / Singleton `IpDetectionCache`

- **Status:** Accepted
- **Date:** 2026-09-01
- **Owner:** Project owner
- **Related artifacts:** [02-ARCHITECTURE.md](02-ARCHITECTURE.md), [PHASE-05-ip-detection.md](PHASE-05-ip-detection.md)

### Context

`PHASE-05-ip-detection.md` requires one external IP lookup to be shared across all due domains within a scheduler tick, and reused by a "check now" call that lands shortly after (FR-13). `IpDetectionService` needs `AppDbContext` (registered `Scoped` by `AddDbContext`) to read `GlobalSettings.ExternalIpCheckServices`, so it must itself be registered `Scoped` — but a cache that only lives as long as one scope can't be shared across ticks/due-domains regardless of how Phase 06's scheduler shapes its DI scopes (still undecided as of this phase).

### Decision

Split the cache into its own class, `IpDetectionCache` (semaphore-guarded, 25s TTL), registered `Singleton` and injected into the `Scoped` `IpDetectionService`. A `Singleton` cannot hold a `Scoped` dependency directly (captive-dependency error), so the cache is deliberately kept free of any `AppDbContext`/EF dependency — it only ever sees the already-resolved `IpDetectionResult` its caller passes it. Only successful lookups are cached; a failed detection is not, so a transient blip on one attempt doesn't force every other caller in the same window to also fail without retrying.

### Boundary Deviation Details

Not a boundary-pattern deviation — both classes stay within `StillHere.Infrastructure`, and `IIpDetectionService`'s public contract (Application-facing) is unaffected by this internal lifetime split.

- **Violated rule:** None.
- **Exact affected scope:** `src/StillHere.Infrastructure/IpDetection/` only.
- **Consequences:** The cache correctly outlives any single DI scope regardless of Phase 06's eventual scheduler-scoping design; adds one extra registered singleton and one extra class versus a simpler (but incorrect for a `Scoped` service) single-class design.
- **Approval:** Project owner, 2026-09-01.
- **Disposition:** Permanent, unless a future phase moves `GlobalSettings` reads off `AppDbContext` entirely (e.g. a config-reload service), at which point `IpDetectionService` could itself become `Singleton` and the split could be collapsed.

### Alternatives Considered

- A single `Singleton` `IpDetectionService` holding `IDbContextFactory<AppDbContext>` instead of `AppDbContext` directly — rejected: no other infrastructure component in this codebase uses `IDbContextFactory`, and introducing it for one class would be inconsistent with the established `AppDbContext`-injection convention (`ManagedDomainRepository`, `AdminUserRepository`) for no benefit beyond what the simpler split already achieves.

### Consequences

- Positive: correct cross-scope cache sharing with no dependency on how Phase 06 shapes its scheduler's DI scopes; the cache is trivially unit-testable in isolation (no DB/HTTP setup needed).
- Negative: one more moving part (two classes instead of one) for what is conceptually a single service.

### Approval

- **Approved by:** Project owner
- **Approved on:** 2026-09-01
