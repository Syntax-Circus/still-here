# 03 - Package Map: still-here

Selected packages are drawn from the Syntax Circus package catalog (`_template/docs/syntaxcircus/PACKAGE_CATALOG.md`, sibling repo). Per the catalog, versions are intentionally not pinned there — this table's **Exact version** and **Source/release verified** columns are filled and locked in `Directory.Packages.props` during Phase 01 (foundation). All versions below were verified directly against nuget.org on 2026-09-01 and are locked in `Directory.Packages.props`.

| Concern | Status | Package | Exact version | Source/release verified | Purpose and boundary | Owning phase |
| --- | --- | --- | --- | --- | --- | --- |
| Transport-neutral operation results | Selected | `SyntaxCircus.Common` | 0.1.3 | [nuget.org/packages/SyntaxCircus.Common/0.1.3](https://www.nuget.org/packages/SyntaxCircus.Common/0.1.3) | `Result`/`Result<T>` for all handler outcomes; `ICurrentUserService` contract via `AddCurrentUserService()` | Phase 01 |
| Structured host logging | Selected | `SyntaxCircus.AspNetCore.Serilog` | 0.1.3 | [nuget.org/packages/SyntaxCircus.AspNetCore.Serilog/0.1.3](https://www.nuget.org/packages/SyntaxCircus.AspNetCore.Serilog/0.1.3) | Console + rolling-file logging setup via `AddStandardSerilog()` | Phase 01 |
| Resilient outbound HTTP | Selected | `SyntaxCircus.Http.Resilience` | 0.1.6 | [nuget.org/packages/SyntaxCircus.Http.Resilience/0.1.6](https://www.nuget.org/packages/SyntaxCircus.Http.Resilience/0.1.6) | Retry/backoff for Namecheap calls, IP-check services, webhook POSTs (replaces raw Polly). Note: a `0.2.0-cmsify.1` prerelease exists on nuget.org but is scoped to another project — stay on the 0.1.x stable line here. | Phase 03, 05, 08 |
| Transactional email | Selected | `SyntaxCircus.Email` | 0.1.5 | [nuget.org/packages/SyntaxCircus.Email/0.1.5](https://www.nuget.org/packages/SyntaxCircus.Email/0.1.5) | `EmailNotificationSender` implementation (replaces direct MailKit usage) | Phase 08 |
| Local credential storage | Selected | `SyntaxCircus.Credentials` | 0.1.1 | [nuget.org/packages/SyntaxCircus.Credentials/0.1.1](https://www.nuget.org/packages/SyntaxCircus.Credentials/0.1.1) | Encrypt/decrypt `DnsProviderCredential.EncryptedSecrets` and SMTP passwords at rest | Phase 04 |
| Reusable Blazor UI components | Selected | `SyntaxCircus.Blazor.Components` | 0.1.2 | [nuget.org/packages/SyntaxCircus.Blazor.Components/0.1.2](https://www.nuget.org/packages/SyntaxCircus.Blazor.Components/0.1.2) | `GlobalErrorBoundary`/`ReconnectModal` (namespace `SyntaxCircus.Blazor.Components.Feedback`) wired into `Components/App.razor` | Phase 01 |
| Development `.env` loading | Selected | `SyntaxCircus.DotEnv` | 0.1.2 | [nuget.org/packages/SyntaxCircus.DotEnv/0.1.2](https://www.nuget.org/packages/SyntaxCircus.DotEnv/0.1.2) | Local dev configuration loading via `ShouldLoadDotEnv`/`AddSyntaxCircusDotEnvFiles` | Phase 01 |
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
| ORM/persistence (runtime) | `Microsoft.EntityFrameworkCore` | 10.0.11 | [nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.11](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.11) | EF Core runtime | Phase 01 |
| ORM/persistence (SQLite provider) | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | [nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.11](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.11) | EF Core SQLite provider | Phase 01 |
| EF migration tooling | `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | [nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.11](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.11) | Design-time services for `dotnet ef migrations` (referenced by both `StillHere.Web`, the startup project, and `StillHere.Infrastructure`); `PrivateAssets=all` so it never flows downstream | Phase 01 |
| Testing framework | `xunit.v3` | 4.0.0 | [nuget.org/packages/xunit.v3/4.0.0](https://www.nuget.org/packages/xunit.v3/4.0.0) | Test framework, runs on Microsoft Testing Platform (`dotnet test` via `OutputType=Exe` + `TestingPlatformDotnetTestSupport=true`, no separate VSTest adapter needed). Chosen over legacy `xunit` 2.x — every other current SyntaxCircus project still uses legacy `xunit`, but nuget.org flags it deprecated/unmaintained; still-here is the first project on v3, confirmed with the project owner. | Phase 01+ (all phases) |
| Test assertions | `Shouldly` | 4.3.0 | [nuget.org/packages/Shouldly/4.3.0](https://www.nuget.org/packages/Shouldly/4.3.0) | Assertion library | Phase 01+ (all phases) |
| Test substitutes | `NSubstitute` | 6.2.0 | [nuget.org/packages/NSubstitute/6.2.0](https://www.nuget.org/packages/NSubstitute/6.2.0) | Mocking/substitute framework (`StillHere.Application.Tests` only — handler tests substitute repository/service interfaces; `StillHere.Infrastructure.Tests` exercises real EF Core against SQLite instead) | Phase 01+ (application-layer test phases) |

All versions are locked in `Directory.Packages.props` as of Phase 01.
