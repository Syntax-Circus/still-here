# 01 - Requirements: still-here

## Problem

The owner runs ~12 personally-owned domains against a home network with a non-static public IP. Maintaining 12 separate DDNS client configs is error-prone and gives no unified visibility. still-here centralizes DDNS management: one dashboard, one audit trail, one notification path.

## Goals and Success Criteria

- Single Docker container, `docker-compose up`, SQLite persisted to a mounted volume.
- Single-admin web login to manage domains and credentials.
- Pluggable DNS provider abstraction, Namecheap shipped in v1.
- Global default polling interval with per-domain override.
- Full audit log of every check, not just updates.
- Configurable webhook + email notifications, per-channel trigger selection.
- Secrets encrypted at rest.
- Success = IP changes detected and pushed within one polling interval; every check/update attempt audited; owner notified same-cycle on change/failure.

## Personas

- **Owner/Admin** (only persona): adds/edits/removes managed domains, reviews the audit log, configures notification channels and global settings, changes their own password.

## Applications and Workers

- **still-here-admin** (Blazor Server, the one user-facing app) — see [UX-BRIEF-still-here-admin.md](UX-BRIEF-still-here-admin.md).
- **Scheduler** (in-process `BackgroundService`, `PeriodicTimer`) — no separate UI, drives all check/update activity.

## Scope

### In Scope

See [PROJECT_BRIEF.md § Scope](../PROJECT_BRIEF.md#scope) — carried forward unchanged.

### Explicitly Out of Scope

- Multi-user/RBAC in v1.
- IPv6 support in v1 (design must not preclude adding it).
- Built-in reverse proxy/TLS termination.
- DNS providers beyond Namecheap in v1.

## Functional Requirements

### Auth

- FR-1: First run with no `AdminUser` row redirects every route to `/setup`.
- FR-2: `/setup` creates exactly one initial admin account (username + hashed password); once an admin exists, `/setup` is unreachable.
- FR-3: `/login` authenticates via cookie auth; all routes except `/setup` and `/login` require `[Authorize]`.
- FR-4: Admin can change their password from `/settings`.

### Domain Management

- FR-5: Admin can add a managed domain: domain name, host, provider selection (drives dynamic credential fields), optional polling interval override, enabled flag.
- FR-6: Admin can edit or delete an existing managed domain.
- FR-7: Admin can enable/disable a domain without deleting it.
- FR-8: Admin can trigger a manual "check now" for a single domain, bypassing its schedule but reusing the same check/update code path.

### DNS Provider Integration

- FR-9: The system supports a pluggable `IDnsProvider` abstraction; adding a new provider requires no changes to scheduler, UI list rendering, or audit logging.
- FR-10: v1 ships a Namecheap provider implementing its DDNS GET-based update API and XML response parsing.

### IP Detection

- FR-11: The system detects the current public IP via an ordered, configurable fallback chain of external check services.
- FR-12: The system compares the provider-reported IP (where available, e.g. Namecheap's response) against the IP sent, logging mismatches as a warning-level audit entry.
- FR-13: One external IP lookup is shared across all due domains within a single scheduler tick.

### Scheduling

- FR-14: The scheduler ticks on a short base interval (~30s) and selects domains due by `PollingIntervalOverrideSeconds ?? GlobalSettings.DefaultPollingIntervalSeconds`.
- FR-15: An unchanged IP writes a `CheckOnly` audit entry only; a changed (or first-run) IP calls the provider's update and writes `IpChanged`/`UpdateFailed`/`UpdateSucceeded` accordingly.
- FR-16: Provider and IP-check calls are wrapped in a retry/backoff policy before being marked failed.

### Audit Log

- FR-17: Every check and every update attempt (success or failure) is recorded with timestamp, domain, old/new IP, status, and message.
- FR-18: Admin can view a per-domain history and a global, filterable (event type/success/date range) audit log.
- FR-19: Audit log retention is configurable (days, or unlimited).

### Notifications

- FR-20: Admin can create/edit/delete webhook and email notification channels.
- FR-21: Each channel independently selects which events trigger it (IP change / failure / success).
- FR-22: Notification send failures are logged to the application log, not the audit log.

### Settings

- FR-23: Admin can configure default polling interval, IP detection mode/fallback chain, audit log retention, and notification channels from `/settings`.

## Non-Functional Requirements

See [PROJECT_BRIEF.md § Non-Functional Requirements](../PROJECT_BRIEF.md#non-functional-requirements) — carried forward unchanged: single low-resource container, negligible scheduler overhead at ~12 domains, best-effort availability, Serilog + `/healthz` observability.

## Assumptions

- The owner fronts the container with their own reverse proxy/TLS; still-here itself only needs to listen on HTTP inside the container.
- ~12 domains is representative scale; the design is not required to scale beyond a small personal domain count.

## Risks

- Namecheap may change its DDNS response XML schema without notice — mitigated by verifying the schema at Phase 03 implementation time (see open questions below).
- A single-admin, no-external-IdP auth model is a deliberate v1 simplification; revisiting it later touches the auth boundary (see [04-DECISION-LOG.md](04-DECISION-LOG.md) #2).

## Open Questions

Carried from [PROJECT_BRIEF.md § Open Questions](../PROJECT_BRIEF.md#open-questions):

- [ ] Exact Namecheap DDNS response XML fields to parse.
- [ ] Whether "check now" should be rate-limited.
- [ ] Final password hasher choice (`PasswordHasher<T>` vs BCrypt.Net).
