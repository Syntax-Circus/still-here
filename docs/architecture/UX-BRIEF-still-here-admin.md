# UX Brief: still-here-admin

**Handoff audience:** Claude Design, UX designer, and implementation team

## Product Context

- **Application purpose:** Single-admin web dashboard for managing Dynamic DNS across the owner's domains — add/edit domains, watch check/update status, review history, configure notifications.
- **Primary business/user outcome:** Replace 12 separate DDNS client configs with one dashboard; give the owner confidence their domains are "still here" (up to date) via visible status and notifications.
- **Related architecture artifact:** [02-ARCHITECTURE.md](02-ARCHITECTURE.md)
- **Design constraints:** Single container, no public-facing exposure assumed (LAN or behind the owner's own reverse proxy); Bootstrap 5 per template default; must render usably on a phone (owner may check status away from home).

## Users and Personas

- **Persona: Owner/Admin**
  - **Goals:** Confirm domains are up to date at a glance; quickly diagnose a failed check; add a new domain in under a minute; not get spammed by notifications.
  - **Pain points (today, pre-still-here):** No unified view across 12 separate DDNS configs; no audit trail when something silently stops working.
  - **Access/permissions:** Full access to everything (single role).

## Key User Flows

### First-Run Setup

- **Starting state:** Fresh container, no `AdminUser` exists.
- **Trigger:** Owner opens the app for the first time.
- **Steps:**
  1. [ ] Every route redirects to `/setup`.
  2. [ ] Owner enters username + password.
  3. [ ] Submit creates the `AdminUser` row and signs the owner in.
- **Success state:** Redirected to `/` (empty dashboard, no domains yet).
- **Failure, loading, and empty states:** Validation errors shown inline; `/setup` becomes unreachable once an admin exists (redirects to `/login` instead).

### Add a Domain

- **Starting state:** Signed in, on `/` or `/domains/add`.
- **Trigger:** Owner clicks "Add domain."
- **Steps:**
  1. [ ] Enter domain name and host.
  2. [ ] Select a DNS provider (Namecheap in v1) — credential fields render dynamically per provider.
  3. [ ] Enter provider credentials, optional polling interval override.
  4. [ ] Submit.
- **Success state:** New domain appears on the dashboard with status `Unknown` until its first check.
- **Failure, loading, and empty states:** Validation errors inline; submit button shows a loading state during save.

### Respond to a Failed Check

- **Starting state:** Dashboard shows a domain with a `Failed` status badge.
- **Trigger:** Owner notices the badge (or received a failure notification).
- **Steps:**
  1. [ ] Click into the domain's history to see the failure message.
  2. [ ] Optionally click "Check now" to retry immediately.
- **Success state:** Status updates to `Ok` on the next successful check.
- **Failure, loading, and empty states:** "Check now" shows a loading state; repeated failures remain visible in history.

### Review Audit Log

- **Starting state:** Signed in.
- **Trigger:** Owner wants to review recent activity (global) or one domain's history.
- **Steps:**
  1. [ ] Navigate to `/audit-log` (global) or `/domains/{id}/history`.
  2. [ ] Filter by event type / success / date range (global view only).
- **Success state:** Paged, filterable table of entries.
- **Failure, loading, and empty states:** Empty state before any checks have run; loading skeleton while paging.

### Configure Notifications

- **Starting state:** Signed in, on `/settings`.
- **Trigger:** Owner wants to be notified of IP changes/failures.
- **Steps:**
  1. [ ] Add a webhook or email channel.
  2. [ ] Choose trigger events (change/failure/success).
  3. [ ] Optionally send a test notification.
- **Success state:** Channel saved and active.
- **Failure, loading, and empty states:** Test-send shows success/failure inline; send failures are logged to the app log, never surfaced as a domain audit entry.

## Screen Inventory

- **Screen/route: `/login`**
  - **Purpose:** Authenticate the admin.
  - **Primary actions:** Submit username/password.
  - **Data/state:** Form state, validation error.
  - **Authorization:** Public (only reachable route pre-auth besides `/setup`).
- **Screen/route: `/setup`**
  - **Purpose:** First-run admin creation.
  - **Primary actions:** Submit initial admin credentials.
  - **Data/state:** Form state, validation error.
  - **Authorization:** Public, but unreachable once an admin exists.
- **Screen/route: `/` (Dashboard)**
  - **Purpose:** At-a-glance status of every managed domain.
  - **Primary actions:** Toggle enabled, "check now," navigate to add/edit/history.
  - **Data/state:** List of domains with last-known IP/status; loading/empty states.
  - **Authorization:** `[Authorize]`.
- **Screen/route: `/domains/add`, `/domains/{id}/edit`**
  - **Purpose:** Create/update a managed domain and its provider credentials.
  - **Primary actions:** Submit, cancel, (edit only) delete.
  - **Data/state:** Form state, dynamic provider credential fields.
  - **Authorization:** `[Authorize]`.
- **Screen/route: `/domains/{id}/history`**
  - **Purpose:** Per-domain audit history with old→new IP diff view.
  - **Primary actions:** Paginate.
  - **Data/state:** Paged audit entries for one domain.
  - **Authorization:** `[Authorize]`.
- **Screen/route: `/audit-log`**
  - **Purpose:** Global, filterable audit history.
  - **Primary actions:** Filter (event type/success/date range), paginate.
  - **Data/state:** Paged, filtered audit entries across all domains.
  - **Authorization:** `[Authorize]`.
- **Screen/route: `/settings`**
  - **Purpose:** Global configuration — polling interval, IP detection mode, retention, notification channels, password change.
  - **Primary actions:** Save each section; add/edit/delete/test-send notification channels.
  - **Data/state:** `GlobalSettings`, list of `NotificationChannel`.
  - **Authorization:** `[Authorize]`.

## Razor Presentation Architecture

For Razor applications, follow [Razor Component Architecture](../RAZOR_COMPONENT_ARCHITECTURE.md) (copied locally from the template). API contracts are DTOs, not ViewModels.

This table is maintained once, in [02-ARCHITECTURE.md § Razor Presentation-Boundary Table](02-ARCHITECTURE.md#razor-presentation-boundary-table) — see there for the current version rather than duplicating it here.

## Interaction and Content Rules

- **Navigation and information hierarchy:** Dashboard is the landing page and hub; every other screen is reachable from it or from `/settings`.
- **Forms and validation:** Inline validation messages next to each field; disable submit while a request is in flight.
- **Notifications and errors:** Toast/inline alert for save success/failure; status badges use color + text label (not color alone).
- **Destructive actions and confirmation:** Domain delete and notification-channel delete require a confirm step.
- **Loading, empty, offline, and degraded states:** Dashboard shows a skeleton while loading, an empty state before any domain is added; Blazor Server reconnect UI via `SyntaxCircus.Blazor.Components` on disconnect.

## Accessibility and Responsive Behavior

- **Keyboard and screen-reader expectations:** All actions (toggle, check-now, delete) reachable and labeled for keyboard/screen-reader use.
- **Color contrast and non-color cues:** Status badges pair color with text (`Ok`/`Unchanged`/`Failed`/`Unknown`), not color alone.
- **Responsive layout behavior:** Dashboard table collapses to a stacked card layout below a small-viewport breakpoint (phone use case from Product Context).
- **Reduced-motion or other preferences:** No motion-heavy UI planned; respect `prefers-reduced-motion` if any transitions are added later.

## Visual Direction

- **Bootstrap 5 components/utilities to prefer:** Tables, badges, forms, toasts/alerts — stock Bootstrap 5, per template default.
- **SCSS variable overrides:** Status-badge color variables only, if needed.
- **Custom SCSS justified only for:** Status badge coloring, if Bootstrap's stock badge variants are insufficient.
- **Typography, imagery, and content tone:** Plain, utilitarian admin-tool tone — no marketing content, no imagery needed.

## Handoff Notes

- **Open design questions:** None beyond the open questions already tracked in [PROJECT_BRIEF.md](../PROJECT_BRIEF.md#open-questions).
- **Prototype/wireframe references:** None yet — this brief is the first design artifact.
- **Acceptance criteria for design review:** Every screen in the Screen Inventory is represented; status badges are color+text; forms show inline validation; destructive actions are confirmed.
