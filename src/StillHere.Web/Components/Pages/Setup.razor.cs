using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace StillHere.Web.Components.Pages;

public partial class Setup
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private SetupViewModel _viewModel = new(null);

    protected override void OnInitialized()
    {
        var query = QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);
        var error = query.TryGetValue("error", out var errorValue) ? errorValue.ToString() : null;

        _viewModel = new SetupViewModel(string.IsNullOrEmpty(error) ? null : error);
    }
}

internal sealed record SetupViewModel(string? ErrorMessage);
