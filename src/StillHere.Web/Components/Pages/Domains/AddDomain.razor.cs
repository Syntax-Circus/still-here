using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;

namespace StillHere.Web.Components.Pages.Domains;

public partial class AddDomain
{
    [Inject]
    private IDnsProviderRegistry DnsProviders { get; set; } = default!;

    [Inject]
    private IAddManagedDomainRequestHandler Handler { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private ManagedDomainFormViewModel _viewModel = new();
    private IReadOnlyList<IDnsProvider> _providers = [];
    private IReadOnlyList<ProviderCredentialField> _credentialFields = [];
    private List<string> _errorMessages = [];
    private bool _isSubmitting;

    protected override void OnInitialized()
    {
        _providers = DnsProviders.Providers;

        if (_providers.Count > 0)
        {
            _viewModel.ProviderKey = _providers[0].ProviderKey;
            OnProviderChanged();
        }
    }

    private void OnProviderChanged()
    {
        var provider = DnsProviders.Providers.FirstOrDefault(p => p.ProviderKey == _viewModel.ProviderKey);
        _credentialFields = provider?.CredentialFields ?? [];
        _viewModel.CredentialSecrets = _credentialFields.ToDictionary(f => f.Key, _ => string.Empty);
    }

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessages = [];

        try
        {
            var request = new AddManagedDomainRequest(
                _viewModel.DomainName,
                _viewModel.Host,
                _viewModel.ProviderKey,
                $"{_viewModel.DomainName} credential",
                _viewModel.CredentialSecrets,
                _viewModel.PollingIntervalOverrideSeconds);

            var result = await Handler.HandleAsync(request, CancellationToken.None);

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
}

internal sealed class ManagedDomainFormViewModel
{
    public string DomainName { get; set; } = "";

    public string Host { get; set; } = "@";

    public string ProviderKey { get; set; } = "";

    public Dictionary<string, string> CredentialSecrets { get; set; } = [];

    public int? PollingIntervalOverrideSeconds { get; set; }

    public bool Enabled { get; set; } = true;
}
