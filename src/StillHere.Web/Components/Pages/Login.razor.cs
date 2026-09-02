using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace StillHere.Web.Components.Pages;

public partial class Login
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private LoginViewModel _viewModel = new(null, "/");

    protected override void OnInitialized()
    {
        var query = QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);
        var error = query.TryGetValue("error", out var errorValue) ? errorValue.ToString() : null;
        var returnUrl = query.TryGetValue("returnUrl", out var returnUrlValue) ? returnUrlValue.ToString() : "/";

        _viewModel = new LoginViewModel(
            string.IsNullOrEmpty(error) ? null : error,
            string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}

internal sealed record LoginViewModel(string? ErrorMessage, string ReturnUrl);
