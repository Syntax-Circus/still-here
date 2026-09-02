using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.AuditLog;

namespace StillHere.Web.Components.Pages.AuditLog;

public partial class AuditLogPage
{
    [Inject]
    private IGetAuditLogEntriesRequestHandler AuditLogHandler { get; set; } = default!;

    private AuditEventKind? _filterEventType;

    // Bound to the <select> as a plain string ("", "true", "false") rather than bool? directly --
    // Blazor's native @bind support for <select> does not reliably round-trip a nullable bool
    // (verified: the underlying field silently never updates from the rendered <option> values).
    private string _filterSuccessValue = "";

    private bool? FilterSuccess => _filterSuccessValue switch
    {
        "true" => true,
        "false" => false,
        _ => null,
    };

    private DateOnly? _filterFromDate;
    private DateOnly? _filterToDate;

    private bool _isLoading = true;
    private string? _errorMessage;
    private IReadOnlyList<AuditLogRowViewModel> _rows = [];
    private int _page = 1;
    private int _totalPages;
    private bool _hasPreviousPage;
    private bool _hasNextPage;

    protected override Task OnInitializedAsync() => LoadPageAsync(1);

    private Task HandleFilterSubmitAsync() => LoadPageAsync(1);

    private async Task LoadPageAsync(int page)
    {
        _isLoading = true;
        _errorMessage = null;

        var request = new GetAuditLogEntriesRequest(
            ManagedDomainId: null,
            _filterEventType,
            FilterSuccess,
            _filterFromDate?.ToDateTime(TimeOnly.MinValue),
            _filterToDate?.ToDateTime(TimeOnly.MaxValue),
            page,
            AuditLogPaging.DefaultPageSize);

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
