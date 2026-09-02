# still-here

*A [Syntax Circus](https://www.syntaxcircus.com) project*

still-here is a self-hosted Dynamic DNS manager: a single Docker container that watches your public IP and pushes updates to your domains' DNS providers (Namecheap in v1), with a full audit log and configurable notifications. It's built for anyone running several personal domains against a connection with a non-static public IP who wants one dashboard instead of a pile of separate DDNS client configs.

## Features

- Manage any number of domains from one dashboard, each pointed at its own DNS provider credential.
- Global default polling interval with an optional per-domain override.
- A full audit log of every check (not just updates), with admin-configurable retention.
- Webhook (Discord, Slack, ntfy, or any generic HTTP endpoint) and SMTP email notifications, with per-channel triggers for IP changes, failures, and successes.
- Single-admin login; provider and SMTP secrets are encrypted at rest.

## Quick start

```bash
docker compose up -d
```

Then open `http://localhost:8080`. The first request redirects you to `/setup` to create the one admin account; after that, log in at `/login` and add your first domain from the dashboard (pick a DNS provider and paste its credential — no environment variables to configure for a basic setup, since provider and SMTP credentials are entered through the UI, not container env vars).

still-here serves plain HTTP inside the container. Put your own reverse proxy (Caddy, Traefik, nginx, etc.) in front of it if you need TLS.

## Data and persistence

`docker-compose.yml` mounts a named volume at `/data`, holding:

- `stillhere.db` — the SQLite database.
- `dataprotection-keys/` — encryption keys for credentials at rest.
- `logs/` — rolling Serilog output.

`/healthz` is the container's healthcheck endpoint.

## Tech stack

.NET 10, Blazor Server (Interactive Server render mode, Bootstrap 5), EF Core + SQLite, Serilog. See [docs/architecture/](docs/architecture/) for the full discovery and design record, including the [implementation roadmap](docs/architecture/99-IMPLEMENTATION-ROADMAP.md).

## Container images

CI publishes multi-arch images to `ghcr.io/syntax-circus/still-here` on every push to `main`, tagged `latest` and with the GitVersion-computed SemVer (e.g. `0.2.0`). This requires the repo to be public, and — separately — the GHCR package's own visibility set to Public after its first publish (GHCR does not inherit repo visibility automatically).

To build locally without pushing:

```powershell
./Build-StillHereDocker.ps1
```

To build and push a multi-arch image to a registry you're already logged into:

```powershell
docker login ghcr.io
./Build-StillHereDocker.ps1 -Registry ghcr.io/syntax-circus -Push
```

**arm64 locally**: building/pushing `linux/arm64` from a Windows/WSL2 dev machine needs QEMU emulation registered in Docker Desktop's builder, which the script doesn't set up itself (mirrors the sibling `Build-*.ps1` scripts). If a build fails with `exec format error` on an arm64 step, register it with:

```powershell
docker run --privileged --rm tonistiigi/binfmt --install all
```

This registration lives in the Docker Desktop/WSL2 VM and does **not** persist across a WSL2 or Docker Desktop restart — rerun the command any time arm64 builds start failing again with the same error. CI doesn't need this; the GitHub Actions workflow registers QEMU itself on every run via `docker/setup-qemu-action`. To skip arm64 locally instead of registering QEMU, pass `-Platforms @('linux/amd64')`.

## Development

```bash
dotnet build
dotnet test
```

Copy [`src/StillHere.Web/.env.example`](src/StillHere.Web/.env.example) to `.env.local` in that same folder to override the database path, data-protection keys path, scheduler intervals, or logging settings for your local run — it's loaded automatically outside a container. Without it, `dotnet run`/Visual Studio use the relative defaults baked into `appsettings.json` (`stillhere.db`, `keys/`, `logs/`), all in `src/StillHere.Web/`.

The [docs/](docs/) directory has the complete design history — [docs/PROJECT_BRIEF.md](docs/PROJECT_BRIEF.md) for scope and requirements, [docs/architecture/00-DISCOVERY-INDEX.md](docs/architecture/00-DISCOVERY-INDEX.md) for the architecture discovery index.

## License

MIT — see [LICENSE](LICENSE).
