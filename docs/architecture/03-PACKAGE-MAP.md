# 03 - Package Map: still-here

Selected packages are drawn from the Syntax Circus package catalog (`_template/docs/syntaxcircus/PACKAGE_CATALOG.md`, sibling repo). Per the catalog, versions are intentionally not pinned there — this table's **Exact version** and **Source/release verified** columns are filled and locked in `Directory.Packages.props` during Phase 01 (foundation); until then they read `TODO`.

| Concern | Status | Package | Exact version | Source/release verified | Purpose and boundary | Owning phase |
| --- | --- | --- | --- | --- | --- | --- |
| Transport-neutral operation results | Selected | `SyntaxCircus.Common` | TODO | TODO | `Result`/`Result<T>` for all handler outcomes; `ICurrentUserService` contract | Phase 01 |
| Structured host logging | Selected | `SyntaxCircus.AspNetCore.Serilog` | TODO | TODO | Console + rolling-file logging setup | Phase 01 |
| Resilient outbound HTTP | Selected | `SyntaxCircus.Http.Resilience` | TODO | TODO | Retry/backoff for Namecheap calls, IP-check services, webhook POSTs (replaces raw Polly) | Phase 03, 05, 08 |
| Transactional email | Selected | `SyntaxCircus.Email` | TODO | TODO | `EmailNotificationSender` implementation (replaces direct MailKit usage) | Phase 08 |
| Local credential storage | Selected | `SyntaxCircus.Credentials` | TODO | TODO | Encrypt/decrypt `DnsProviderCredential.EncryptedSecrets` and SMTP passwords at rest | Phase 04 |
| Reusable Blazor UI components | Selected | `SyntaxCircus.Blazor.Components` | TODO | TODO | Error boundary, not-found, and reconnect UI for the admin app | Phase 01 |
| Development `.env` loading | Selected | `SyntaxCircus.DotEnv` | TODO | TODO | Local dev configuration loading | Phase 01 |
| EF Core/Postgres conventions | Not applicable | `SyntaxCircus.EntityFrameworkCore.Postgres` | — | — | Project uses SQLite, not Postgres (see [04-DECISION-LOG.md](04-DECISION-LOG.md) #1) | — |
| Blazor token forwarding/session mgmt | Not applicable | `SyntaxCircus.Blazor.Auth` | — | — | No external IdP/OAuth token flow — single local admin, custom cookie auth (see [04-DECISION-LOG.md](04-DECISION-LOG.md) #2) | — |
| API authentication | Not applicable | `SyntaxCircus.AspNetCore.Authentication` | — | — | No public API surface to authenticate | — |
| API middleware / ProblemDetails mapping | Not applicable | `SyntaxCircus.AspNetCore.Common` | — | — | No public API endpoints beyond `/healthz` | — |
| Message correlation | Not applicable | `SyntaxCircus.AspNetCore.Common.MassTransit` | — | — | No message bus in this topology | — |
| Blazor metadata/SEO | Not applicable | `SyntaxCircus.Blazor.Seo` | — | — | Internal-only admin tool, not publicly indexed | — |
| Consent-aware analytics | Not applicable | `SyntaxCircus.Blazor.Tracking` | — | — | Single-owner personal tool, no analytics need | — |
| Decorative Blazor visual effects | Not applicable | `SyntaxCircus.FancyBlazor` | — | — | No decorative UI requirement for an admin dashboard | — |
| File/blob storage abstraction | Not applicable | `SyntaxCircus.Storage` | — | — | No file/blob storage beyond the SQLite DB and log files | — |
| AI provider clients | Not applicable | `SyntaxCircus.AI.Providers` | — | — | No AI features | — |
| Cmsify CMS client (+ caching) | Not applicable | `SyntaxCircus.Cmsify.Client`, `.DistributedCaching` | — | — | No CMS content | — |
| MAUI secure token storage | Not applicable | `SyntaxCircus.Maui.TokenStorage` | — | — | No MAUI client | — |
| RevenueCat integration (+ MAUI) | Not applicable | `SyntaxCircus.RevenueCat`, `.Maui` | — | — | No subscriptions/IAP | — |

## Non-SyntaxCircus Packages (No Catalog Equivalent)

| Concern | Package | Exact version | Source/release verified | Purpose | Owning phase |
| --- | --- | --- | --- | --- | --- |
| ORM/persistence | `Microsoft.EntityFrameworkCore.Sqlite` | TODO | TODO | EF Core SQLite provider | Phase 01 |
| Testing framework | `xunit` | TODO | TODO | Test framework | Phase 01+ (all phases) |
| Test assertions | `Shouldly` | TODO | TODO | Assertion library | Phase 01+ (all phases) |
| Test substitutes | `NSubstitute` | TODO | TODO | Mocking/substitute framework | Phase 01+ (all phases) |

All `TODO` cells are resolved in Phase 01 by checking each package's current stable release on NuGet.org/GitHub and locking the version in `Directory.Packages.props`.
