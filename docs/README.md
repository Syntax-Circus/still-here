# still-here

*A Syntax Circus project (www.syntaxcircus.com)*

still-here is a self-hosted Dynamic DNS manager: a single Docker container that watches your public IP and pushes updates to each of your domains' DNS providers (starting with Namecheap), with a full audit log and configurable notifications.

## Start Here

1. [docs/PROJECT_BRIEF.md](PROJECT_BRIEF.md) — the intake brief (identity, scope, requirements seed, tech preferences).
2. [docs/architecture/00-DISCOVERY-INDEX.md](architecture/00-DISCOVERY-INDEX.md) — the index of the full discovery output: requirements, architecture, package map, decision log, UX brief, and phase-by-phase implementation plan.
3. [docs/APPLICATION_ARCHITECTURE.md](APPLICATION_ARCHITECTURE.md) and [docs/RAZOR_COMPONENT_ARCHITECTURE.md](RAZOR_COMPONENT_ARCHITECTURE.md) — mandatory server-side and Razor boundary rules, copied unmodified from the Syntax Circus project template (`_template`, sibling repo).

This structure and process follow the [Syntax Circus project template](../../_template)'s discovery workflow (`_template/docs/ARCHITECTURE_DISCOVERY.md`): no implementation code is created before the architecture in `docs/architecture/` is approved, and implementation proceeds one explicitly-selected phase at a time.
