# 02 - Architecture: still-here

## Selected Topology

- One Blazor Server host process (still-here-admin) containing:
  - The Blazor Server UI (Interactive Server render mode).
  - The scheduler `BackgroundService`.
  - EF Core + SQLite persistence, in the same process/container.
- No separate API project, no separate worker process, no message bus — a single deployable per [PROJECT_BRIEF.md](../PROJECT_BRIEF.md)'s topology.

## Component Responsibilities

| Component | Responsibility |
| --- | --- |
| Blazor pages/components | Thin presentation entry points; bind input, delegate to named handlers, render outcomes |
| Named use-case handlers | Application logic per use case (see boundary table below) |
| `IDnsProvider` implementations | Provider-specific update calls (Namecheap in v1) |
| `IIpDetectionService` | External IP fallback chain + provider-reported IP comparison |
| Scheduler (`BackgroundService`) | Ticks, selects due domains, invokes handlers |
| `INotificationSender` implementations | Webhook/email delivery |
| EF Core `AppDbContext` + repositories | SQLite persistence, entity mapping |
| `ICredentialProtector` (via `SyntaxCircus.Credentials`) | Encrypt/decrypt provider and SMTP secrets at rest |

## Data Flows

1. Scheduler tick → `RunScheduledDomainCheckHandler` → `IIpDetectionService` (shared lookup) → per due domain, compare to `LastKnownIp` → unchanged: audit `CheckOnly`; changed: `IDnsProvider.UpdateAsync` → audit `IpChanged`/`UpdateFailed`/`UpdateSucceeded` → `INotificationSender` fan-out on matching triggers.
2. Blazor page action (e.g. add domain) → handler → repository → SQLite → `Result<T>` → page renders outcome.
3. Manual "check now" → the same handler used by the scheduler path (`RunManualDomainCheckRequestHandler` reuses the shared check/update logic), invoked synchronously from a button click instead of a tick.

## Data Model (EF Core Entities)

Carried forward from the original plan, unchanged:

**`AdminUser`** — `Id`, `Username`, `PasswordHash`, `PasswordSalt`, `CreatedAtUtc`, `LastLoginAtUtc`.

**`DnsProviderCredential`** — `Id`, `ProviderKey`, `Name`, `EncryptedSecrets` (JSON, encrypted via `SyntaxCircus.Credentials`), `CreatedAtUtc`.

**`ManagedDomain`** — `Id`, `DomainName`, `Host`, `ProviderCredentialId` (FK), `Enabled`, `PollingIntervalOverrideSeconds` (nullable), `LastKnownIp` (nullable), `LastCheckedAtUtc`, `LastUpdatedAtUtc`, `LastStatus` (enum: `Unknown`/`Ok`/`Unchanged`/`Failed`), `CreatedAtUtc`.

**`AuditLogEntry`** — `Id`, `ManagedDomainId` (FK, nullable), `TimestampUtc`, `EventType` (enum: `CheckOnly`, `IpChanged`, `UpdateFailed`, `UpdateSucceeded`, `DomainAdded`, `DomainEdited`, `DomainDeleted`, `LoginSuccess`, `LoginFailure`), `OldIp` (nullable), `NewIp` (nullable), `Message`, `Success`.

**`GlobalSettings`** (single-row) — `DefaultPollingIntervalSeconds`, `IpDetectionMode`, `ExternalIpCheckServices` (JSON ordered list), `AuditLogRetentionDays` (nullable).

**`NotificationChannel`** — `Id`, `Type` (`Webhook`/`Email`), `Name`, `Enabled`, per-type config fields (webhook: `Url`, `BodyTemplate`, `HttpMethod`; email: `SmtpHost`, `SmtpPort`, `UseSsl`, `Username`, `EncryptedPassword`, `FromAddress`, `ToAddresses`), `TriggerOnIpChange`, `TriggerOnFailure`, `TriggerOnSuccess`.

## Authentication and Authorization

Custom single-admin cookie auth — see [04-DECISION-LOG.md § Decision 2](04-DECISION-LOG.md#decision-2-custom-single-admin-cookie-auth-instead-of-oauthauthentik). `AdminUser` remains a real table (not an env-var check) to keep a future multi-user migration low-effort. First-run `/setup` gate; `[Authorize]` on every route except `/setup`/`/login`. Identity is exposed to handlers via `ICurrentUserService` (`SyntaxCircus.Common` or a project-local equivalent) — handlers never read cookies/claims directly.

## Persistence

EF Core 10 + SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite` — no matching SyntaxCircus package, since `SyntaxCircus.EntityFrameworkCore.Postgres` doesn't apply; see [03-PACKAGE-MAP.md](03-PACKAGE-MAP.md)). Migrations generated via EF tooling only, never handwritten. Repository interfaces expose application-oriented operations only — no `IQueryable`/`DbSet`/EF entities leak past the infrastructure boundary.

## Integrations

See [01-REQUIREMENTS.md](01-REQUIREMENTS.md) and [PROJECT_BRIEF.md § Integrations](../PROJECT_BRIEF.md#integrations): Namecheap DDNS API, external IP-check services, outbound webhooks, SMTP.

## Deployment

Single multi-stage Dockerfile (SDK build → `aspnet:10.0` runtime), `docker-compose.yml` with one service (`still-here`), one named `/data` volume holding `stillhere.db`, `dataprotection-keys/`, and `logs/`, port mapping, `restart: unless-stopped`, healthcheck against `/healthz`. No secrets baked into the image or compose file.

## Decision References

See [04-DECISION-LOG.md](04-DECISION-LOG.md):

1. SQLite instead of Postgres.
2. Custom single-admin cookie auth instead of OAuth/Authentik.
3. Full handler-per-entry-point pattern applied despite small app size.

## Application Boundary Table

Per [Application Architecture](../APPLICATION_ARCHITECTURE.md) (copied locally from the template).

| Entry point/use case | Named handler | Application dependencies | Infrastructure implementations | Outcome mapping | Tests | Decision |
| --- | --- | --- | --- | --- | --- | --- |
| `/login` submit | `AuthenticateAdminRequestHandler` | `IAdminUserRepository`, `IPasswordHasher`, `ICurrentUserService` | EF repository, ASP.NET cookie sign-in (infra-owned) | `Result<AuthenticatedAdminDto>` → sets auth cookie or shows validation error | Handler: substituted repo/hasher. Entry point: cookie sign-in delegation | — |
| `/setup` submit | `CreateInitialAdminRequestHandler` | `IAdminUserRepository`, `IPasswordHasher` | EF repository | `Result<AdminUserDto>` → redirect to `/` or show error; blocked if an admin already exists | Handler: no-admin and admin-exists branches | — |
| `/domains/add` submit | `AddManagedDomainRequestHandler` | `IManagedDomainRepository`, `IDnsProviderRegistry`, `ICredentialProtector` | EF repository, `SyntaxCircus.Credentials` | `Result<ManagedDomainDto>` | Handler: validation, provider-field mismatch | — |
| `/domains/{id}/edit` submit | `UpdateManagedDomainRequestHandler` | `IManagedDomainRepository`, `ICredentialProtector` | EF repository, `SyntaxCircus.Credentials` | `Result<ManagedDomainDto>` | Handler: not-found, validation | — |
| Domain delete action | `DeleteManagedDomainRequestHandler` | `IManagedDomainRepository` | EF repository | `Result` | Handler: not-found | — |
| "Check now" button | `RunManualDomainCheckRequestHandler` | `IManagedDomainRepository`, `IIpDetectionService`, `IDnsProviderRegistry`, `IAuditLogWriter`, `INotificationDispatcher` | EF repository, `IDnsProvider` impls, HTTP clients, notification senders | `Result<DomainCheckOutcomeDto>` | Handler: unchanged/changed/failure branches, substituted deps | — |
| `/settings` save | `UpdateGlobalSettingsRequestHandler` | `IGlobalSettingsRepository` | EF repository | `Result` | Handler: validation | — |
| Notification channel add | `CreateNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result<NotificationChannelDto>` | Handler: validation | — |
| Notification channel edit | `UpdateNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result<NotificationChannelDto>` | Handler: not-found, validation | — |
| Notification channel delete | `DeleteNotificationChannelRequestHandler` | `INotificationChannelRepository` | EF repository | `Result` | Handler: not-found | — |
| Change password | `ChangeAdminPasswordRequestHandler` | `IAdminUserRepository`, `IPasswordHasher`, `ICurrentUserService` | EF repository | `Result` | Handler: wrong-current-password branch | — |
| Dashboard load | `GetDashboardSummaryRequestHandler` | `IManagedDomainRepository` | EF repository | `Result<DashboardSummaryDto>` | Handler: empty state | — |
| Audit log / domain history load | `GetAuditLogEntriesRequestHandler` | `IAuditLogRepository` | EF repository | `Result<PagedResult<AuditLogEntryDto>>` | Handler: filter/paging logic | — |
| Scheduler tick (`BackgroundService`, constructor-injected per the hosted-job exception) | `RunScheduledDomainCheckHandler` | Same as "check now" | Same as "check now" | `Task` (fire-and-forget per domain; no caller branches on outcome) | Handler: due-domain selection, shared IP-lookup caching | — |
| `/healthz` | *(exempt — framework-owned operational endpoint)* | — | — | Executes no application workflow | — | — |

## Razor Presentation-Boundary Table

Per [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md) (copied locally from the template).

| Feature/page | Component pair decision | ViewModel or direct model | Factory/presentation-service decision | State ownership and behavior | API DTO boundary |
| --- | --- | --- | --- | --- | --- |
| `/login` | Paired (injection, async submit) | `LoginViewModel` (feature-local) | None needed | Component owns form/validation state | N/A — no external API |
| `/setup` | Paired (injection, async submit) | `SetupViewModel` | None needed | Component owns form state | N/A |
| `/` Dashboard | Paired (injection, async load, periodic refresh) | `DashboardRowViewModel` per domain | Factory for domain→row mapping (multiple fields, status badge logic) | Component owns loading/empty/error state | N/A |
| `/domains/add`, `/domains/{id}/edit` | Paired (injection, dynamic provider-driven fields, async submit) | `ManagedDomainFormViewModel` | Factory to build dynamic credential-field list from `IDnsProviderRegistry` | Component owns form state, dynamic field list | N/A |
| `/domains/{id}/history` | Paired (injection, async paged load) | `AuditLogRowViewModel` | None needed (direct DTO→ViewModel mapping) | Component owns paging state | N/A |
| `/audit-log` | Paired (injection, async paged/filtered load) | `AuditLogRowViewModel` (shared with history) | None needed | Component owns filter/paging state | N/A |
| `/settings` | Paired (injection, multiple sub-sections, async submit) | `SettingsViewModel`, `NotificationChannelViewModel` | Factory for channel-type-specific field shaping | Component owns per-section form state | N/A |
| Status badge (dashboard row) | Inline (simple `Status` param, no callback) | — | — | Stateless | N/A |
| "Check now" button | Inline (simple param + one trivial `EventCallback` forwarder) | — | — | Stateless | N/A |

## Docker

Carried forward from the original plan, unchanged: image/service name `still-here`, multi-stage Dockerfile (SDK build → `aspnet:10.0` runtime), single `/data` volume (`stillhere.db`, `dataprotection-keys/`, `logs/`), `docker-compose.yml` with port mapping, volume, `restart: unless-stopped`, `/healthz` healthcheck, no secrets baked into image/compose.
