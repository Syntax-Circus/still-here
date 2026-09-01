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
