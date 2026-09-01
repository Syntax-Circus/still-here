# 00 - Discovery Index: still-here

**Status:** Discovery complete — pending human approval to begin Phase 01.

## Artifacts

| Artifact | Purpose | Status |
| --- | --- | --- |
| [../PROJECT_BRIEF.md](../PROJECT_BRIEF.md) | Intake brief: identity, scope, requirements seed, tech preferences | Complete |
| [01-REQUIREMENTS.md](01-REQUIREMENTS.md) | Formal requirements: goals, personas, scope, functional/non-functional requirements | Complete |
| [02-ARCHITECTURE.md](02-ARCHITECTURE.md) | Topology, data model, application boundary table, Razor boundary table, deployment | Complete |
| [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md) | Package selection status, versions (TODO), boundaries, owning phase | Complete (versions pending Phase 01) |
| [04-DECISION-LOG.md](04-DECISION-LOG.md) | Recorded/approved architecture deviations | Complete |
| [UX-BRIEF-still-here-admin.md](UX-BRIEF-still-here-admin.md) | Designer/implementation handoff for the admin app | Complete |
| [PHASE-01-foundation.md](PHASE-01-foundation.md) … [PHASE-09-polish.md](PHASE-09-polish.md) | Ordered implementation phases | Complete |
| [99-IMPLEMENTATION-ROADMAP.md](99-IMPLEMENTATION-ROADMAP.md) | Phase order, dependencies, status | Complete |

## Phase Order

1. PHASE-01-foundation
2. PHASE-02-auth
3. PHASE-03-dns-provider-abstraction
4. PHASE-04-domain-management-ui
5. PHASE-05-ip-detection
6. PHASE-06-scheduler
7. PHASE-07-dashboard-audit-log
8. PHASE-08-notifications
9. PHASE-09-polish

Dependencies are linear except where noted (Phases 03 and 05 can run in parallel — see [99-IMPLEMENTATION-ROADMAP.md](99-IMPLEMENTATION-ROADMAP.md) for the full graph).

## Open Decisions

All material decisions identified during discovery are recorded and approved in [04-DECISION-LOG.md](04-DECISION-LOG.md):

1. SQLite instead of Postgres.
2. Custom single-admin cookie auth instead of OAuth/Authentik.
3. Full handler-per-entry-point pattern applied despite small app size.

No decisions remain open. Three open *questions* (not architecture decisions) are tracked in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions) and resurface in the relevant phase docs.

## Approval Status

- [ ] Human approval to begin Phase 01 (this checkbox is the implementation gate — do not start Phase 01 until checked by the project owner).

## Completion Checklist

Mirrors `_template/docs/ARCHITECTURE_DISCOVERY.md`'s Completion Checklist.

- [x] Goals and measurable success criteria are explicit.
- [x] Personas, applications, workers, and explicit non-scope are identified.
- [x] Security, data, scale, availability, and deployment constraints are resolved or marked unknown.
- [x] Auth, authorization, persistence, migrations, retention, and operations are addressed.
- [x] Relevant cross-cutting concerns were reviewed against the package catalog.
- [ ] Every selected package has an exact version and versioned source/release verification link *(deferred — recorded as TODO in 03-PACKAGE-MAP.md, resolved in Phase 01)*.
- [ ] The foundation phase creates `Directory.Packages.props` and locks selected package versions centrally *(scheduled — see PHASE-01-foundation.md)*.
- [x] Material decisions are recorded and approved in the decision log.
- [x] Every application use-case entry point maps to one named use-case handler.
- [x] Every exempt operational or static endpoint executes no application workflow (`/healthz` only).
- [x] Handler dependencies were reviewed for HTTP, EF, concrete infrastructure, SDK, and transport leaks.
- [x] Result/exception semantics and transport mapping are explicit for each use case.
- [x] Every boundary deviation has an approved decision-log entry.
- [x] Every Razor feature/page records its inline-or-paired component decision.
- [x] Every Razor feature/page records its feature-local ViewModel or direct model decision.
- [x] Razor loading, error, empty, and mutable state ownership and behavior are explicit.
- [x] Repeated or business-meaningful literals are named constants at the right scope.
- [x] Duplicated-looking logic across flows was evaluated for genuine divergence.
- [x] Phase dependencies are acyclic and each phase has testable criteria.
- [x] Every user-facing application has a UX brief (one: still-here-admin).
- [x] No implementation artifacts were created before architecture approval (`src/` remains empty).
