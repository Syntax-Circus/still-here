# Project Brief: still-here

Filled from the original informal plan and subsequent clarification with the project owner. `sample_prompt.md`'s stack baseline is a technology preference, not an assumed topology — see the deviations called out below and recorded in `docs/architecture/04-DECISION-LOG.md`.

## Project Identity

- **Name:** still-here
- **One-sentence problem:** The owner runs ~12 personally-owned domains pointed at a home network with a non-static public IP, and wants one dashboard to manage Dynamic DNS updates across all of them instead of maintaining 12 separate DDNS client configs.
- **Desired outcome and measurable success criteria:** A single self-hosted container that (a) detects public IP changes within one polling interval, (b) successfully pushes updates to each domain's DNS provider with retries on transient failure, (c) records a complete audit trail of every check/update, and (d) notifies the owner within the same cycle when a change or failure occurs.
- **Owner/stakeholders:** Repo owner (single admin/operator); Syntax Circus.
- **Target release or milestones:** v1 = Namecheap provider only, single admin, no IPv6. See `docs/architecture/99-IMPLEMENTATION-ROADMAP.md` for phase order.

## Users and Selected Topology

- **Personas:** Owner/Admin — sole user; adds domains, reviews the audit log, configures notifications.
- **User-facing applications:** One Blazor Server admin web app (the only application in this project).
- **Internal/admin applications:** None separate — the admin app *is* the whole UI surface.
- **Background workers or scheduled jobs:** One in-process `BackgroundService` scheduler (`PeriodicTimer`) that periodically checks IP and triggers DNS provider updates.
- **Expected client platforms:** Desktop/mobile browser only.
- **Deployment topology/environments:** Single Docker container (`docker-compose up`), SQLite persisted to a mounted `/data` volume. No separate DB service, no separate API service. The user supplies their own reverse proxy/TLS (Caddy/Traefik/nginx) — out of scope for this container.

## Scope

### In Scope

- [x] Single Docker container deployment via docker-compose, SQLite-backed.
- [x] Single-admin web login (username/password), first-run `/setup` flow.
- [x] Pluggable DNS provider abstraction; Namecheap shipped in v1, structured for Cloudflare later.
- [x] Global default polling interval with optional per-domain override.
- [x] Full audit log of every check (not just updates).
- [x] Configurable webhook + email notifications, per-channel trigger selection (IP change / failure / success).
- [x] Secrets (DDNS passwords/API tokens) encrypted at rest.

### Explicitly Out of Scope

- [ ] Multi-user / RBAC (v1 is single-admin only; `AdminUser` modeled as a table so this is a low-effort future migration).
- [ ] IPv6 support (Namecheap's DDNS is IPv4-only today; the IP model must not preclude adding it later).
- [ ] Built-in reverse proxy / TLS termination (assumes the user fronts the container themselves).
- [ ] DNS providers beyond Namecheap in v1 (Cloudflare etc. deferred, but the abstraction must not block adding them).

## Functional Requirements

- [ ] Admin can log in/out; first run forces creation of the initial admin account.
- [ ] Admin can add, edit, enable/disable, and delete a managed domain, choosing a DNS provider and entering its required credential fields.
- [ ] The scheduler checks each enabled domain's effective public IP at its effective polling interval and updates the provider only when the IP has changed (or on first run).
- [ ] Every check (changed or not) and every provider update attempt (success or failure) is written to the audit log.
- [ ] Admin can trigger a manual "check now" for a single domain outside its normal schedule.
- [ ] Admin can view a per-domain history and a global, filterable audit log.
- [ ] Admin can configure webhook and/or email notification channels and choose which events trigger each.
- [ ] Admin can change the admin password and adjust global settings (default polling interval, IP detection mode/fallback chain, audit log retention).

## Integrations

| System | Purpose | Direction | Authentication | Notes |
| --- | --- | --- | --- | --- |
| Namecheap DDNS API | Push IP updates for managed domains | Outbound | Per-domain DDNS password (provider-issued) | XML response; v1's only DNS provider |
| External IP-check services (ifconfig.me, api.ipify.org, icanhazip.com) | Detect current public IP | Outbound | None | Ordered fallback chain, short timeouts |
| Outbound webhook targets (Discord/Slack/ntfy/generic) | Notify on IP change/failure | Outbound | Per-channel (URL, optional templated body) | User-editable JSON body template |
| SMTP server | Email notifications | Outbound | Per-channel SMTP credentials | User-configured, not a fixed provider |

## Data and Security

- **Primary data stores:** SQLite file (`stillhere.db`) on a mounted `/data` volume.
- **Sensitive data:** DNS provider credentials/secrets, SMTP credentials, admin password hash.
- **Retention/deletion requirements:** Audit log retention is admin-configurable (`AuditLogRetentionDays`, nullable = keep forever).
- **Authentication provider:** None external — custom local single-admin cookie auth (see `04-DECISION-LOG.md` #2).
- **Authorization model:** Single role (admin); `[Authorize]` on every route except `/setup` and `/login`.
- **Compliance or residency requirements:** None (self-hosted personal tool).

## Non-Functional Requirements

- **Expected users/traffic:** One concurrent user, low request volume.
- **Performance targets:** Scheduler tick overhead negligible at ~12 domains; one shared external-IP lookup per tick regardless of due-domain count.
- **Availability/recovery targets:** Best-effort; `restart: unless-stopped`; no HA requirement for a home-hosted tool.
- **Observability requirements:** Serilog to console (`docker logs`) + rolling file in the data volume; `/healthz` endpoint for compose healthcheck.
- **Budget or operational constraints:** Must run comfortably as a single low-resource container on home/self-hosted infrastructure.

## Technology Preferences

Record only preferences known at intake. The discovery process validates them
against project needs and package boundaries.

- **Framework/runtime:** .NET 10.
- **Frontend:** Blazor Server, Interactive Server render mode, Bootstrap 5.
- **API style:** None — no public API; a single Blazor Server host handles UI and the in-process scheduler.
- **Database:** SQLite — deviates from the template's Postgres sample default; see `docs/architecture/04-DECISION-LOG.md` #1.
- **Authentication:** Custom single-admin cookie auth — deviates from the template's OAuth/Authentik sample default; see `docs/architecture/04-DECISION-LOG.md` #2.
- **Logging:** Serilog, via `SyntaxCircus.AspNetCore.Serilog`.
- **Testing:** xunit.v3 (Microsoft Testing Platform) + Shouldly + NSubstitute.
- **Styling/design system:** Bootstrap 5 (template default).
- **Configuration/secrets:** `.env`/`.env.local` for dev via `SyntaxCircus.DotEnv`; ASP.NET Data Protection API + `SyntaxCircus.Credentials` for provider secrets at rest in production.
- **Syntax Circus packages to consider:** `SyntaxCircus.Common`, `SyntaxCircus.AspNetCore.Serilog`, `SyntaxCircus.Http.Resilience`, `SyntaxCircus.Email`, `SyntaxCircus.Credentials`, `SyntaxCircus.Blazor.Components`, `SyntaxCircus.DotEnv` — see `docs/architecture/03-PACKAGE-MAP.md` for the full selection with statuses.

## Open Questions

- [ ] Exact Namecheap DDNS response XML fields to parse for provider-reported IP — verify against Namecheap's current docs during Phase 03.
- [ ] Should "check now" be rate-limited to prevent accidental repeated-click hammering of the provider API?
- [x] Final choice of password hasher: ASP.NET Identity's `PasswordHasher<T>` (used standalone), resolved in Phase 02 — see `docs/architecture/02-ARCHITECTURE.md § Authentication and Authorization`.
