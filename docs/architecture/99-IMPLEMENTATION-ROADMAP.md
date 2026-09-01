# 99 - Implementation Roadmap: still-here

Ordered phase index, dependencies, and status. Implementation starts only when the project owner explicitly selects a phase (see [00-DISCOVERY-INDEX.md § Approval Status](00-DISCOVERY-INDEX.md#approval-status)).

| # | Phase | Depends on | Status |
| --- | --- | --- | --- |
| 1 | [PHASE-01-foundation](PHASE-01-foundation.md) | — | Complete |
| 2 | [PHASE-02-auth](PHASE-02-auth.md) | Phase 01 | Complete |
| 3 | [PHASE-03-dns-provider-abstraction](PHASE-03-dns-provider-abstraction.md) | Phase 01 | Complete |
| 4 | [PHASE-04-domain-management-ui](PHASE-04-domain-management-ui.md) | Phase 02, Phase 03 | Complete |
| 5 | [PHASE-05-ip-detection](PHASE-05-ip-detection.md) | Phase 01 | Complete |
| 6 | [PHASE-06-scheduler](PHASE-06-scheduler.md) | Phase 03, Phase 04, Phase 05 | Not started |
| 7 | [PHASE-07-dashboard-audit-log](PHASE-07-dashboard-audit-log.md) | Phase 04, Phase 06 | Not started |
| 8 | [PHASE-08-notifications](PHASE-08-notifications.md) | Phase 06 | Not started |
| 9 | [PHASE-09-polish](PHASE-09-polish.md) | Phase 07, Phase 08 | Not started |

Phase 03 (provider abstraction) and Phase 05 (IP detection) can proceed in parallel after Phase 01 — both depend only on foundation, not on each other or on auth.

## Validation Commands

Recorded per-phase in each `PHASE-NN-*.md`'s Success Criteria; at minimum, every phase from Phase 01 onward must pass `dotnet build` and `dotnet test` before being marked done.

## Phase-Selection Handoff

To begin implementation: the project owner explicitly names one phase document (starting with `PHASE-01-foundation.md`) as approved to implement. No phase begins without that explicit selection — see `_template/AGENT_GUIDE.md` and `_template/docs/ARCHITECTURE_DISCOVERY.md § Stage 5`.
