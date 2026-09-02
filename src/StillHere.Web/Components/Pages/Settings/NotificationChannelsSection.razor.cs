using Microsoft.AspNetCore.Components;
using StillHere.Application.Features.Notifications;

namespace StillHere.Web.Components.Pages.Settings;

public partial class NotificationChannelsSection
{
    [Inject]
    private INotificationChannelRepository NotificationChannels { get; set; } = default!;

    [Inject]
    private ICreateNotificationChannelRequestHandler CreateHandler { get; set; } = default!;

    [Inject]
    private IUpdateNotificationChannelRequestHandler UpdateHandler { get; set; } = default!;

    [Inject]
    private IDeleteNotificationChannelRequestHandler DeleteHandler { get; set; } = default!;

    [Inject]
    private ITestNotificationChannelRequestHandler TestHandler { get; set; } = default!;

    private IReadOnlyList<NotificationChannelDto> _channels = [];
    private bool _isLoading = true;
    private List<string> _errorMessages = [];
    private string? _successMessage;

    private bool _isFormVisible;
    private int? _editingChannelId;
    private NotificationChannelViewModel _formViewModel = new();
    private bool _isSubmitting;

    private int? _confirmingDeleteId;

    private readonly HashSet<int> _testingChannelIds = [];
    private readonly Dictionary<int, (bool Success, string Message)> _testResultsByChannelId = [];

    protected override Task OnInitializedAsync() => LoadChannelsAsync();

    private async Task LoadChannelsAsync()
    {
        _isLoading = true;
        _channels = await NotificationChannels.ListAllAsync(CancellationToken.None);
        _isLoading = false;
    }

    private void ShowAddForm()
    {
        _formViewModel = new NotificationChannelViewModel();
        _editingChannelId = null;
        _errorMessages = [];
        _successMessage = null;
        _isFormVisible = true;
    }

    private void ShowEditForm(NotificationChannelDto channel)
    {
        _formViewModel = NotificationChannelViewModelFactory.Create(channel);
        _editingChannelId = channel.Id;
        _errorMessages = [];
        _successMessage = null;
        _isFormVisible = true;
    }

    private void CancelForm()
    {
        _isFormVisible = false;
        _errorMessages = [];
    }

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessages = [];
        _successMessage = null;

        try
        {
            var result = _editingChannelId is int id
                ? await UpdateHandler.HandleAsync(
                    new UpdateNotificationChannelRequest(
                        id,
                        _formViewModel.Type,
                        _formViewModel.Name,
                        _formViewModel.Enabled,
                        _formViewModel.Url,
                        _formViewModel.BodyTemplate,
                        _formViewModel.HttpMethod,
                        _formViewModel.SmtpHost,
                        _formViewModel.SmtpPort,
                        _formViewModel.UseSsl,
                        _formViewModel.Username,
                        _formViewModel.Password,
                        _formViewModel.FromAddress,
                        _formViewModel.ToAddresses,
                        _formViewModel.TriggerOnIpChange,
                        _formViewModel.TriggerOnFailure,
                        _formViewModel.TriggerOnSuccess),
                    CancellationToken.None)
                : await CreateHandler.HandleAsync(
                    new CreateNotificationChannelRequest(
                        _formViewModel.Type,
                        _formViewModel.Name,
                        _formViewModel.Enabled,
                        _formViewModel.Url,
                        _formViewModel.BodyTemplate,
                        _formViewModel.HttpMethod,
                        _formViewModel.SmtpHost,
                        _formViewModel.SmtpPort,
                        _formViewModel.UseSsl,
                        _formViewModel.Username,
                        _formViewModel.Password,
                        _formViewModel.FromAddress,
                        _formViewModel.ToAddresses,
                        _formViewModel.TriggerOnIpChange,
                        _formViewModel.TriggerOnFailure,
                        _formViewModel.TriggerOnSuccess),
                    CancellationToken.None);

            if (result.IsFailure)
            {
                _errorMessages = [.. result.Errors.Select(e => e.Message)];
                return;
            }

            _successMessage = _editingChannelId is null ? "Channel added." : "Channel updated.";
            _isFormVisible = false;
            await LoadChannelsAsync();
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void RequestDelete(int id)
    {
        _confirmingDeleteId = id;
    }

    private void CancelDelete()
    {
        _confirmingDeleteId = null;
    }

    private async Task HandleDeleteAsync(int id)
    {
        var result = await DeleteHandler.HandleAsync(new DeleteNotificationChannelRequest(id), CancellationToken.None);

        _confirmingDeleteId = null;

        if (result.IsFailure)
        {
            _errorMessages = [.. result.Errors.Select(e => e.Message)];
            return;
        }

        _successMessage = "Channel deleted.";
        await LoadChannelsAsync();
    }

    private bool IsTesting(int channelId) => _testingChannelIds.Contains(channelId);

    private (bool Success, string Message)? GetTestResult(int channelId) =>
        _testResultsByChannelId.TryGetValue(channelId, out var result) ? result : null;

    private async Task HandleTestAsync(int channelId)
    {
        _testingChannelIds.Add(channelId);
        _testResultsByChannelId.Remove(channelId);

        try
        {
            var result = await TestHandler.HandleAsync(new TestNotificationChannelRequest(channelId), CancellationToken.None);

            _testResultsByChannelId[channelId] = result.IsFailure
                ? (false, result.Errors[0].Message)
                : (true, "Test notification sent successfully.");
        }
        finally
        {
            _testingChannelIds.Remove(channelId);
        }
    }

    private static string FormatTriggers(NotificationChannelDto channel)
    {
        string?[] triggers =
        [
            channel.TriggerOnIpChange ? "IP change" : null,
            channel.TriggerOnFailure ? "Failure" : null,
            channel.TriggerOnSuccess ? "Success" : null,
        ];

        return string.Join(", ", triggers.Where(t => t is not null));
    }
}
