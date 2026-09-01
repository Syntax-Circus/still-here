using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.Domains;

public partial class EditDomain
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    private IManagedDomainRepository ManagedDomains { get; set; } = default!;

    [Inject]
    private IDnsProviderRegistry DnsProviders { get; set; } = default!;

    [Inject]
    private IUpdateManagedDomainRequestHandler UpdateHandler { get; set; } = default!;

    [Inject]
    private IDeleteManagedDomainRequestHandler DeleteHandler { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private ManagedDomainFormViewModel _viewModel = new();
    private IReadOnlyList<ProviderCredentialField> _credentialFields = [];
    private string _providerDisplayName = "";
    private List<string> _errorMessages = [];
    private bool _isSubmitting;
    private bool _notFound;
    private bool _confirmingDelete;

    protected override async Task OnParametersSetAsync()
    {
        var domain = await ManagedDomains.FindByIdAsync(Id, CancellationToken.None);
        if (domain is null)
        {
            _notFound = true;
            return;
        }

        var provider = DnsProviders.Providers.FirstOrDefault(p => p.ProviderKey == domain.ProviderKey);
        _providerDisplayName = provider?.DisplayName ?? domain.ProviderKey;
        _credentialFields = provider?.CredentialFields ?? [];

        _viewModel = new ManagedDomainFormViewModel
        {
            DomainName = domain.DomainName,
            Host = domain.Host,
            ProviderKey = domain.ProviderKey,
            Enabled = domain.Enabled,
            PollingIntervalOverrideSeconds = domain.PollingIntervalOverrideSeconds,
            CredentialSecrets = _credentialFields.ToDictionary(f => f.Key, _ => string.Empty),
        };
    }

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessages = [];

        try
        {
            var request = new UpdateManagedDomainRequest(
                Id,
                _viewModel.DomainName,
                _viewModel.Host,
                _viewModel.Enabled,
                _viewModel.PollingIntervalOverrideSeconds,
                _viewModel.CredentialSecrets);

            var result = await UpdateHandler.HandleAsync(request, CancellationToken.None);

            if (result.IsFailure)
            {
                _errorMessages = [.. result.Errors.Select(e => e.Message)];
                return;
            }

            Navigation.NavigateTo("/");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task HandleDeleteAsync()
    {
        var result = await DeleteHandler.HandleAsync(new DeleteManagedDomainRequest(Id), CancellationToken.None);

        if (result.IsSuccess)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
            _errorMessages = [.. result.Errors.Select(e => e.Message)];
            _confirmingDelete = false;
        }
    }
}
