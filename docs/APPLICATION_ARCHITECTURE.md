# Syntax Circus Application Architecture

*Copied unmodified from the Syntax Circus project template (`_template/docs/APPLICATION_ARCHITECTURE.md`). This is canonical policy — do not edit locally; if a change is needed, propose it in the template repo.*

## Naming Conventions

- Method and constructor parameters use camelCase (`request`,
  `cancellationToken`, `widgets`) — standard C# convention.
- Record/DTO properties stay PascalCase, including a positional record's
  declared parameter, since that identifier *is* the generated property name
  (`CreateWidgetRequest(string Name)`, accessed as `request.Name`).
  Lowercasing a positional parameter to satisfy the parameter-naming rule
  above would lowercase the public property too — don't do that.
- Private fields use underscore-prefixed camelCase (`_currentUser`,
  `_validSorts`), including private `static readonly` fields — not bare
  camelCase or PascalCase.

## Constants Over Magic Values

A literal string or number that appears more than once, or that encodes a
meaningful business rule (a retry count, a page size, a status code), must be
a named constant, a `static class` of constants, or an enum — not a bare
repeated literal.

Scope the constant to the narrowest project that needs it. Co-locate it with
the type or domain it belongs to — nested on the entity it describes, for
example — when it is used within one project. Promote it to a shared
constants location only when the value genuinely crosses a process or client
boundary, such as a status string a server sets and multiple clients branch
on.

The same literal *value* appearing in two unrelated domains is not a
duplicate to collapse. `"pending"` as a purchase status and `"pending"` as a
webhook-event status describe different state machines that happen to reuse
an English word; each domain keeps its own constants class even where values
coincidentally overlap. Merging them coupled two domains that should be free
to diverge.

```csharp
// Crosses a process/client boundary: the API sets these, a Blazor Server app
// and a MAUI app both branch on them. Lives in a shared constants project.
public static class PurchaseConfirmStatuses
{
    public const string NotCompleted = "not_completed";
    public const string Confirmed = "confirmed";
    public const string RateLimited = "rate_limited";
}

// Used by one project only. Stays nested on the entity it describes instead
// of moving to a shared location.
public sealed class Purchase
{
    public static class PurchaseStatuses
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
    }

    public string Status { get; set; } = PurchaseStatuses.Pending;
}
```

The same rule applies to magic numbers. `RevenueCatConfirmRetryPolicy.MaxAttempts`
and `.Delay` replaced two independently hardcoded retry loops — a Blazor
Server page and a MAUI Blazor Hybrid page — whose literal values had drifted
into agreement only by coincidence, not by design.

## Duplication and Shared Abstractions

Before extracting a shared abstraction over near-identical code in two or
more places, ask: if a future change to this logic is driven by a business
rule specific to only one of the flows, would the other flow need to change
too? If no, it is legitimate divergence — leave it duplicated, with a short
comment explaining why it is not shared. If yes, it is real duplication —
extract it.

- **Left separate**: `ConfirmedRevenueCatPurchaseService` vs.
  `ConfirmedAccusationPurchaseService` are surface-similar — both confirm a
  RevenueCat purchase — but rate limiting, suspension/block checks, and
  single- vs. dual-entity creation are genuinely different business rules. A
  shared base would only relocate `if (isAccusation)` branching into the
  "shared" code.
- **Extracted, mechanical duplication**: `IRevenueCatWebCheckoutBridge`
  replaced byte-identical JS-module lifecycle and checkout plumbing
  duplicated across four Blazor pages. The duplication had already silently
  drifted — four independently bumped JS module version constants — a
  concrete sign this was unowned copy-paste, not intentional divergence.
- **Extracted, pure data**: `SagaStateBase` holds the 18 of roughly 21
  properties two saga-state entities shared verbatim, as an unmapped CLR base
  with no persistence or behavior of its own. The message-bus framework's own
  state-machine DSL classes were deliberately left unmerged, since their
  `Event<T>`/`State` shapes do not parameterize cleanly across the two flows'
  differently shaped messages.

Reject a shared base class or service built to unify two flows that only
coincidentally look alike today, rather than because a change to one
genuinely implies the same change to the other. A base riddled with `if
(isX)` branches or fields only one subtype ever populates is evidence the
abstraction was forced.

This is advisory judgment, not a boundary rule: it does not require a
`docs/architecture/04-DECISION-LOG.md` entry, which is reserved for the
layering/boundary deviations covered elsewhere in this document. A one-line
code comment explaining "why not shared" is sufficient.

## Mandatory Dependency Flow

Every server-side use case follows this runtime call flow:

```text
Controller / minimal API / worker / consumer / scheduled job
                            |
                            v
                 Named use-case handler
                            |
                            v
       Service / provider / repository abstractions
                            |
                            v
              Infrastructure implementations
                            |
                            v
          DbContext / SDK / filesystem / network
```

Source dependencies point inward: host projects reference application code, and
infrastructure projects implement and reference application contracts while
owning concrete persistence and integration details. Host composition roots may
reference both application contracts and infrastructure assemblies to select
and register implementations. Application code must not reference
infrastructure implementations.

## Entry Points

Application use-case entry points—controllers, minimal APIs, hosted workers,
scheduled jobs, webhooks, and message consumers—are host or transport adapters.
They may bind input and perform transport-shape validation, apply host-level
authentication and authorization policies, pass cancellation to one named
use-case handler, and map its outcome to transport semantics. Framework-owned
operational or static endpoints, such as health, metrics, or static assets, are
exempt only when they execute no application workflow. This exemption never
permits business behavior, database access, or infrastructure coordination at
the edge.

Entry points must not implement business workflows, query a database, call
concrete infrastructure, or coordinate multiple repositories or providers.
Multiple entry points may share a handler only when they represent the same use
case.

## Named Use-Case Handlers

Each use case has a clearly named, application-owned handler. Request-shaped
handlers use specific names such as `CreateWidgetRequestHandler`; event and
process flows use names such as `ProcessSubscriptionRenewalHandler`.

Handlers accept application-owned request models, with no MVC attributes or
transport objects, coordinate the use case and its business rules, and
propagate cancellation to asynchronous dependencies.

Each handler is exposed through a matching interface (`ICreateWidgetRequestHandler`),
and callers depend on the interface, not the concrete class. For a controller
or minimal API, inject that interface as a `[FromServices]` parameter on the
action method rather than through the constructor. Use constructor injection
only when the same handler instance is genuinely reused by more than one entry
point in the same class. Host types without per-method DI — message consumers,
hosted/scheduled jobs — keep constructor injection, since it is their only
option. Do not introduce a service locator or generic dispatch framework
merely to resolve routine handlers.

## Allowed and Forbidden Dependencies

Handlers may depend on application-facing service, provider, repository,
current-user, clock, storage, messaging, and similar interfaces. A repository
interface is a valid direct handler dependency when it expresses the required
operation cleanly; do not add a pass-through service with no behavior.

Handlers must not depend on `DbContext`, `DbSet`, EF query types, migrations,
persistence entities, concrete infrastructure implementations, vendor SDK
clients, filesystem implementations, raw network clients, `HttpContext`,
controllers, headers, routes, status codes, `IActionResult`, queue
acknowledgement types, scheduler-specific types, or transport response types.

## Identity and Request Context

Handlers obtain identity through `ICurrentUserService` from
`SyntaxCircus.Common` or a project-specific equivalent. This abstraction may
expose the authenticated subject and other application-relevant identity data;
its ASP.NET implementation owns `HttpContext` and claims parsing.

Application code does not read route values, headers, cookies, or claims
directly. The entry point maps required transport data into the application
request model, while the current-user abstraction supplies authenticated
identity. Resource ownership and other business authorization belong in the
handler or its dependencies; host policies stay at the entry point.

## Results and Transport Mapping

Expected application outcomes use `Result` or `Result<T>` from
`SyntaxCircus.Common` when callers must branch on success, validation,
not-found, conflict, authentication, authorization, or general failure.
Results remain transport-neutral: stable error codes, error kinds, client-safe
messages, and validation targets are application concerns, while status codes
and response shapes are adapter concerns.

Transport-shape validation and host policies stay at the adapter. Business
validation and resource authorization stay in the handler or its dependencies.
Unexpected programming, infrastructure, and availability failures remain
exceptions for centralized handling or transport retries.

An internal handler with no meaningful expected negative outcome may return
`Task`. Known business outcomes must still be represented as results, not
exceptions.

```csharp
public sealed record CreateWidgetRequest(string Name);

public sealed record WidgetDto(int Id, string Name);

public interface IWidgetRepository
{
    Task<WidgetDto> CreateAsync(
        string name,
        string userId,
        CancellationToken cancellationToken);
}

public interface ICreateWidgetRequestHandler
{
    Task<Result<WidgetDto>> HandleAsync(
        CreateWidgetRequest request,
        CancellationToken cancellationToken);
}

public sealed class CreateWidgetRequestHandler(
    IWidgetRepository widgets,
    ICurrentUserService currentUser) : ICreateWidgetRequestHandler
{
    public async Task<Result<WidgetDto>> HandleAsync(
        CreateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (!currentUser.IsAuthenticated || userId is null)
        {
            return Result<WidgetDto>.Failure(new ResultError(
                "authentication-required",
                "Authentication is required.",
                ResultErrorKind.Unauthenticated));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<WidgetDto>.Failure(new ResultError(
                "name-required",
                "A name is required.",
                ResultErrorKind.Validation,
                "name"));
        }

        var widget = await widgets.CreateAsync(
            request.Name,
            userId,
            cancellationToken);

        return Result<WidgetDto>.Success(widget);
    }
}

[HttpPost]
public async Task<IActionResult> Create(
    CreateWidgetRequest request,
    [FromServices] ICreateWidgetRequestHandler createWidget,
    CancellationToken cancellationToken)
{
    var result = await createWidget.HandleAsync(request, cancellationToken);

    return result.ToActionResult(
        this,
        widget => CreatedAtAction(nameof(Get), new { id = widget.Id }, widget));
}
```

API controllers map `Result` values at the transport edge and explicitly select
the successful response, such as `Ok`, `CreatedAtAction`, `Accepted`, or
`NoContent`.

## Infrastructure and Persistence

Infrastructure owns EF contexts, entities, queries, migrations, repository and
provider implementations, vendor SDKs, HTTP clients, filesystem access,
messaging transports, platform APIs, and mapping between integration or
persistence shapes and application/domain models.

Repository and provider interfaces expose application-oriented operations and
models. They must not expose `IQueryable`, EF entities, `DbSet`, vendor response
types, or other implementation details. An application-facing unit-of-work
abstraction is allowed only for genuinely atomic multi-write use cases; the
concrete transaction remains infrastructure-owned.

## Background Work and Message Consumers

Workers, scheduled jobs, webhooks, and message consumers follow the same
entry-point-to-handler flow. This consumer mapping is representative of a host;
it does not prescribe this exact retry API.

```csharp
public sealed record SubscriptionRenewalRejected(string[] ErrorCodes);

public async Task Consume(ConsumeContext<SubscriptionRenewed> context)
{
    var result = await processRenewal.HandleAsync(
        new ProcessSubscriptionRenewalRequest(context.Message.SubscriptionId),
        context.CancellationToken);

    if (result.IsFailure)
    {
        await context.RespondAsync(new SubscriptionRenewalRejected(
            result.Errors.Select(error => error.Code).ToArray()));
    }
}
```

The consumer owns acknowledgement and retry semantics while the handler remains
transport-neutral. The host maps expected outcomes to its acknowledgement,
response, retry, or scheduler behavior.

For consumers, injecting the handler by interface happens through the
constructor rather than `[FromServices]`, since message-bus frameworks have no
per-method DI equivalent — this is the one entry-point type where constructor
injection of the handler is the default, not the exception.

A fire-and-forget event consumer (no caller waiting on a typed response) may
use a handler returning plain `Task` instead of `Result`/`Result<T>`: an
"expected" negative branch — a referenced record no longer exists, a
downstream call was ambiguous — is handled by publishing a different
follow-up event or logging and returning, not by a `Result` a caller inspects.
Reserve `Result<T>` for consumers that respond to the message (as above) or
otherwise have a caller that branches on the outcome.

**Saga state machines are exempt from the handler pattern.** A state machine
built entirely from the message-bus framework's own declarative DSL (state
transitions, inline field assignment on the saga's own instance, publishing
follow-up events) with no `DbContext`/repository/service dependencies *is*
simultaneously the entry point and the process logic — there is nothing to
extract into a separate handler. If a saga's `.Then()` block starts calling
repositories or services to do real work, that block has grown into a use
case and belongs in a handler the saga delegates to.

## Testing by Boundary

- Entry-point tests cover delegation, cancellation propagation, authorization
  integration, and transport mapping.
- Handler tests use substitutes for interfaces and no real database or network.
- Infrastructure integration tests cover concrete persistence or integration
  behavior and model mapping.
- Architecture review checks project references and constructor dependencies for
  forbidden outward dependencies.

## Anti-Patterns

Reject these shortcuts:

- A controller or minimal API querying `DbContext` directly.
- A handler depending on `IHttpContextAccessor`.
- A repository or provider interface exposing `DbSet` or `IQueryable`.
- Routine service-location of handlers instead of direct injection.
- A handler with no interface, or a controller depending on a concrete
  handler class instead of its interface.
- Constructor-injecting a handler used by only one action instead of binding
  it as a `[FromServices]` parameter on that action.
- Pass-through services that add no behavior merely to preserve a layering
  shape.
- A repeated literal standing in for a named constant, especially one that
  encodes a business rule (retry counts, page sizes, status codes).
- A shared base class or service built to unify two flows that only
  coincidentally look alike today, rather than because a change to one
  genuinely implies the same change to the other.

## Approved Deviations

Before implementation, record and approve every departure in
`docs/architecture/04-DECISION-LOG.md`. The entry must identify the rule, its
rationale, exact scope, consequences for security, testing, coupling, and
migration, and a removal condition or an explicit statement of permanence.
Convenience, deadline pressure, or avoiding a small interface is not sufficient
on its own.

## Review Checklist

- Is the entry point thin and limited to adapter responsibilities?
- Does it delegate to one named use-case handler through its interface?
- Is the handler bound as a `[FromServices]` action parameter, unless it is
  genuinely reused by more than one entry point in the same class?
- Are request and output types application-owned and transport-neutral?
- Does the handler depend only on allowed abstractions?
- Are concrete infrastructure and forbidden transport dependencies absent?
- Is cancellation propagated through the handler and its asynchronous calls?
- Are expected outcomes mapped from `Result` at the transport edge with an
  explicit success response?
- Do entry-point, handler, and infrastructure tests each verify their own
  boundary?
- Does every deviation link to an approved decision-log entry?
- Are repeated or business-meaningful literals named constants, scoped to the
  narrowest project that needs them?
- Was duplicated-looking code evaluated for genuine divergence before being
  extracted into (or left out of) a shared abstraction?
