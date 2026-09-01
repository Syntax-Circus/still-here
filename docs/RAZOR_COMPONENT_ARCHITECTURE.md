# Razor Component Architecture

*Copied unmodified from the Syntax Circus project template (`_template/docs/RAZOR_COMPONENT_ARCHITECTURE.md`). This is canonical policy — do not edit locally; if a change is needed, propose it in the template repo.*

## Scope and Authority

This guide is the canonical policy for Razor component presentation code. It
applies to Razor components in application projects and complements
[APPLICATION_ARCHITECTURE.md](APPLICATION_ARCHITECTURE.md): server-side use
cases still enter through application-facing clients or presentation
abstractions rather than data-access or infrastructure types.

## Component File Boundaries

Keep a component inline only when it contains simple parameters and, at most,
one trivial synchronous `EventCallback`-forwarding callback. A trivial callback
only forwards a supplied synchronous `EventCallback`; it has no injected
dependency, asynchronous work, navigation, helper method, lifecycle behavior,
or mutable component state.

Use paired `.razor` and `.razor.cs` files for every other component. This
includes components with injection, lifecycle or asynchronous work, mutable
state, navigation, helper methods, multiple callbacks, JS interop, or
disposal/subscriptions. When a component is paired, keep all of its C# in the
code-behind file: injection, parameters, lifecycle work, state, callbacks,
navigation, and helpers. The `.razor` file contains markup and bindings only.

## Razor ViewModels

ViewModels are Razor-only presentation models. Define them in the feature that
owns the component, near that feature's components and presentation code.
Name them for their presentation purpose, such as
`ProductDetailsViewModel`; do not promote them to shared API contracts.

Use a factory or presentation service only when presentation shaping involves
non-trivial mapping, asynchronous assembly, or multiple dependencies. Simple
feature-local construction belongs in the component code-behind.

## API DTO Boundary

API contracts use DTO names and contracts, such as `ProductDto` or
`ProductDetailsResponse`. They are not ViewModels. Do not call API request or
response types ViewModels, and do not expose Razor ViewModels through an API.

The MarketTracker API ViewModel naming is a historical anti-pattern: it blurs
the API and presentation boundaries this guide preserves.

## Data and Dependency Flow

Component code-behind may depend on application-facing client or presentation
abstractions. It must never depend on `DbContext`, EF types, or concrete
infrastructure. A client or presentation abstraction supplies application or
DTO data; the code-behind performs only simple local presentation shaping, or
delegates justified complex shaping to a feature-local factory or presentation
service.

```text
Razor markup -> component code-behind -> client/presentation abstraction
                                      -> application boundary
```

## Examples

An inline component stays within the ceiling because it has simple parameters
and only forwards one supplied synchronous callback:

```razor
@* ProductRow.razor *@
<button type="button" @onclick="OnSelected">@Name</button>

@code {
    [Parameter, EditorRequired] public string Name { get; set; } = "";
    [Parameter] public EventCallback OnSelected { get; set; }
}
```

Use a paired component when it needs injection, lifecycle work, state, or a
feature-local ViewModel:

```razor
@* ProductDetails.razor *@
@if (_viewModel is null)
{
    <p>Loading product…</p>
}
else
{
    <article>
        <h1>@_viewModel.Name</h1>
        <p>@_viewModel.DisplayPrice</p>
    </article>
}
```

```csharp
// ProductDetails.razor.cs
public partial class ProductDetails
{
    [Inject] private IProductClient Products { get; set; } = default!;

    [Parameter] public int ProductId { get; set; }

    private ProductDetailsViewModel? _viewModel;

    protected override async Task OnParametersSetAsync()
    {
        var product = await Products.GetAsync(ProductId);
        _viewModel = new ProductDetailsViewModel(
            product.Name,
            $"{product.Price:C}");
    }
}

internal sealed record ProductDetailsViewModel(string Name, string DisplayPrice);
```

If `GetAsync` data must be combined with other dependencies, assembled
asynchronously from several sources, or mapped non-trivially, move that work to
a feature-local factory or presentation service rather than expanding the
component.

## Review Checklist

- Is an inline component limited to simple parameters plus at most one trivial,
  synchronous `EventCallback`-forwarding callback?
- Does every component beyond that ceiling use paired `.razor` and `.razor.cs`
  files, with all C# in code-behind?
- Is each ViewModel Razor-only and feature-local?
- Is a factory or presentation service present only for non-trivial mapping,
  asynchronous assembly, or multiple dependencies?
- Are API contracts named DTOs rather than ViewModels?
- Does code-behind depend only on application-facing client or presentation
  abstractions, not `DbContext`, EF types, or concrete infrastructure?

## Reference Patterns

SlotShark is the positive reference for paired Razor components, feature-local
presentation models, and explicit boundaries. ChatHarvester inline logic is a
historical anti-pattern: it demonstrates why component logic beyond the inline
ceiling belongs in code-behind. MarketTracker API ViewModel naming is likewise
a historical anti-pattern because API contracts are DTOs, not Razor ViewModels.
