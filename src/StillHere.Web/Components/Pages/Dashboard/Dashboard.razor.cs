using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.Dashboard;
using StillHere.Application.Features.DomainChecks;
using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.Dashboard;

public partial class Dashboard
{
    [Inject]
    private IGetDashboardSummaryRequestHandler SummaryHandler { get; set; } = default!;

    [Inject]
    private IUpdateManagedDomainRequestHandler UpdateHandler { get; set; } = default!;

    [Inject]
    private IRunManualDomainCheckRequestHandler CheckHandler { get; set; } = default!;

    private IReadOnlyList<DashboardRowViewModel> _rows = [];
    private readonly HashSet<int> _checkingDomainIds = [];
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override Task OnInitializedAsync() => LoadDashboardAsync();

    private async Task LoadDashboardAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        var result = await SummaryHandler.HandleAsync(CancellationToken.None);
        if (result.IsFailure)
        {
            _errorMessage = result.Errors[0].Message;
        }
        else
        {
            _rows = [.. result.Value.Domains.Select(DashboardRowViewModelFactory.Create)];
        }

        _isLoading = false;
    }

    private async Task HandleCheckNowAsync(int domainId)
    {
        _checkingDomainIds.Add(domainId);

        try
        {
            var result = await CheckHandler.HandleAsync(new ManualDomainCheckRequest(domainId), CancellationToken.None);
            if (result.IsFailure)
            {
                _errorMessage = result.Errors[0].Message;
            }

            await LoadDashboardAsync();
        }
        finally
        {
            _checkingDomainIds.Remove(domainId);
        }
    }

    private async Task HandleToggleEnabledAsync(DashboardRowViewModel row, bool newEnabled)
    {
        var request = new UpdateManagedDomainRequest(
            row.Id, row.DomainName, row.Host, newEnabled, row.PollingIntervalOverrideSeconds, CredentialSecrets: null);

        var result = await UpdateHandler.HandleAsync(request, CancellationToken.None);
        if (result.IsFailure)
        {
            _errorMessage = result.Errors[0].Message;
            return;
        }

        await LoadDashboardAsync();
    }
}
