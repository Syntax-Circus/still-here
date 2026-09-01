using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.AuditLog;
using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.AuditLog;

public partial class DomainHistory
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    private IManagedDomainRepository ManagedDomains { get; set; } = default!;

    [Inject]
    private IGetAuditLogEntriesRequestHandler AuditLogHandler { get; set; } = default!;

    private string? _domainName;
    private bool _notFound;
    private bool _isLoading = true;
    private string? _errorMessage;
    private IReadOnlyList<AuditLogRowViewModel> _rows = [];
    private int _page = 1;
    private int _totalPages;
    private bool _hasPreviousPage;
    private bool _hasNextPage;

    protected override async Task OnParametersSetAsync()
    {
        var domain = await ManagedDomains.FindByIdAsync(Id, CancellationToken.None);
        if (domain is null)
        {
            _notFound = true;
            _isLoading = false;
            return;
        }

        _domainName = domain.DomainName;
        await LoadPageAsync(1);
    }

    private async Task LoadPageAsync(int page)
    {
        _isLoading = true;
        _errorMessage = null;

        var request = new GetAuditLogEntriesRequest(
            Id, EventType: null, Success: null, FromUtc: null, ToUtc: null, page, AuditLogPaging.DefaultPageSize);

        var result = await AuditLogHandler.HandleAsync(request, CancellationToken.None);
        if (result.IsFailure)
        {
            _errorMessage = result.Errors[0].Message;
        }
        else
        {
            _rows = [.. result.Value.Items.Select(AuditLogRowViewModelFactory.Create)];
            _page = result.Value.Page;
            _totalPages = result.Value.TotalPages;
            _hasPreviousPage = result.Value.HasPreviousPage;
            _hasNextPage = result.Value.HasNextPage;
        }

        _isLoading = false;
    }
}
