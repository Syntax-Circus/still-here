using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.Auth;

namespace StillHere.Web.Components.Pages.Settings;

public partial class PasswordChangeSection
{
    [Inject]
    private IChangeAdminPasswordRequestHandler Handler { get; set; } = default!;

    private PasswordChangeFormViewModel _viewModel = new();
    private List<string> _errorMessages = [];
    private string? _successMessage;
    private bool _isSubmitting;

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessages = [];
        _successMessage = null;

        try
        {
            var request = new ChangeAdminPasswordRequest(
                _viewModel.CurrentPassword,
                _viewModel.NewPassword,
                _viewModel.ConfirmNewPassword);

            var result = await Handler.HandleAsync(request, CancellationToken.None);

            if (result.IsFailure)
            {
                _errorMessages = [.. result.Errors.Select(e => e.Message)];
                return;
            }

            _successMessage = "Password changed.";
            _viewModel = new PasswordChangeFormViewModel();
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}

internal sealed class PasswordChangeFormViewModel
{
    public string CurrentPassword { get; set; } = "";

    public string NewPassword { get; set; } = "";

    public string ConfirmNewPassword { get; set; } = "";
}
