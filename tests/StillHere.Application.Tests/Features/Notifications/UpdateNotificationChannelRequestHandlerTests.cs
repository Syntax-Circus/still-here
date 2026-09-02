using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Application.Security;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class UpdateNotificationChannelRequestHandlerTests
{
    private readonly INotificationChannelRepository _notificationChannels = Substitute.For<INotificationChannelRepository>();
    private readonly ISmtpCredentialProtector _smtpCredentialProtector = Substitute.For<ISmtpCredentialProtector>();
    private readonly UpdateNotificationChannelRequestHandler _handler;

    private static readonly NotificationChannelDto ExistingWebhook = new(
        1, NotificationChannelType.Webhook, "My Webhook", true, "https://example.com/hook", null, "POST",
        null, null, false, null, null, null, null, true, false, false);

    private static readonly NotificationChannelDto ExistingEmail = new(
        2, NotificationChannelType.Email, "My Email Channel", true, null, null, null,
        "smtp.example.com", 587, true, "user@example.com", "old-encrypted-password",
        "from@example.com", "to@example.com", true, true, false);

    public UpdateNotificationChannelRequestHandlerTests()
    {
        _handler = new UpdateNotificationChannelRequestHandler(_notificationChannels, _smtpCredentialProtector);
    }

    private static UpdateNotificationChannelRequest ValidWebhookRequest() => new(
        1,
        NotificationChannelType.Webhook,
        "My Webhook",
        Enabled: true,
        Url: "https://example.com/hook",
        BodyTemplate: null,
        HttpMethod: null,
        SmtpHost: null,
        SmtpPort: null,
        UseSsl: false,
        Username: null,
        Password: null,
        FromAddress: null,
        ToAddresses: null,
        TriggerOnIpChange: true,
        TriggerOnFailure: false,
        TriggerOnSuccess: false);

    private static UpdateNotificationChannelRequest ValidEmailRequest(string? password = null) => new(
        2,
        NotificationChannelType.Email,
        "My Email Channel",
        Enabled: true,
        Url: null,
        BodyTemplate: null,
        HttpMethod: null,
        SmtpHost: "smtp.example.com",
        SmtpPort: 587,
        UseSsl: true,
        Username: "user@example.com",
        Password: password,
        FromAddress: "from@example.com",
        ToAddresses: "to@example.com",
        TriggerOnIpChange: true,
        TriggerOnFailure: true,
        TriggerOnSuccess: false);

    [Fact]
    public async Task HandleAsync_ChannelNotFound_ReturnsNotFound()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((NotificationChannelDto?)null);

        var result = await _handler.HandleAsync(ValidWebhookRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.NotFound);
        result.Errors[0].Code.ShouldBe("notification-channel-not-found");
    }

    [Fact]
    public async Task HandleAsync_TypeDiffersFromExisting_ReturnsValidationErrorAndDoesNotUpdate()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);

        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { Type = NotificationChannelType.Email }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("channel-type-immutable");
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Validation);
        await _notificationChannels.DidNotReceive().UpdateAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingName_ReturnsValidationError()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);

        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { Name = "" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("name-required");
    }

    [Fact]
    public async Task HandleAsync_WebhookMissingUrl_ReturnsValidationError()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);

        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { Url = "" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("url-required");
    }

    [Fact]
    public async Task HandleAsync_WebhookBlankHttpMethod_DefaultsToPost()
    {
        _notificationChannels.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingWebhook);
        _notificationChannels.UpdateAsync(
                1, "My Webhook", true,
                "https://example.com/hook", null, "POST",
                null, null, false, null, null,
                null, null,
                true, false, false,
                Arg.Any<CancellationToken>())
            .Returns(ExistingWebhook);

        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { HttpMethod = "" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _notificationChannels.Received(1).UpdateAsync(
            1, "My Webhook", true,
            "https://example.com/hook", null, "POST",
            null, null, false, null, null,
            null, null,
            true, false, false,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, 587, "from@example.com", "to@example.com", "smtp-host-required")]
    [InlineData("", 587, "from@example.com", "to@example.com", "smtp-host-required")]
    [InlineData("smtp.example.com", null, "from@example.com", "to@example.com", "smtp-port-required")]
    [InlineData("smtp.example.com", 0, "from@example.com", "to@example.com", "smtp-port-required")]
    [InlineData("smtp.example.com", 587, null, "to@example.com", "from-address-required")]
    [InlineData("smtp.example.com", 587, "", "to@example.com", "from-address-required")]
    [InlineData("smtp.example.com", 587, "from@example.com", null, "to-addresses-required")]
    [InlineData("smtp.example.com", 587, "from@example.com", "", "to-addresses-required")]
    public async Task HandleAsync_EmailMissingRequiredField_ReturnsValidationError(
        string? smtpHost, int? smtpPort, string? fromAddress, string? toAddresses, string expectedCode)
    {
        _notificationChannels.FindByIdAsync(2, Arg.Any<CancellationToken>()).Returns(ExistingEmail);

        var request = ValidEmailRequest() with
        {
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            FromAddress = fromAddress,
            ToAddresses = toAddresses,
        };

        var result = await _handler.HandleAsync(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task HandleAsync_BlankPassword_PassesNullNewEncryptedPassword()
    {
        _notificationChannels.FindByIdAsync(2, Arg.Any<CancellationToken>()).Returns(ExistingEmail);
        _notificationChannels.UpdateAsync(
                2, "My Email Channel", true,
                null, null, null,
                "smtp.example.com", 587, true, "user@example.com", null,
                "from@example.com", "to@example.com",
                true, true, false,
                Arg.Any<CancellationToken>())
            .Returns(ExistingEmail);

        var result = await _handler.HandleAsync(ValidEmailRequest(password: ""), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _smtpCredentialProtector.DidNotReceive().Protect(Arg.Any<string>());
        await _notificationChannels.Received(1).UpdateAsync(
            2, "My Email Channel", true,
            null, null, null,
            "smtp.example.com", 587, true, "user@example.com", null,
            "from@example.com", "to@example.com",
            true, true, false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonBlankPassword_EncryptsAndPassesNewEncryptedPassword()
    {
        _notificationChannels.FindByIdAsync(2, Arg.Any<CancellationToken>()).Returns(ExistingEmail);
        _smtpCredentialProtector.Protect("new-password").Returns("new-encrypted-password");
        _notificationChannels.UpdateAsync(
                2, "My Email Channel", true,
                null, null, null,
                "smtp.example.com", 587, true, "user@example.com", "new-encrypted-password",
                "from@example.com", "to@example.com",
                true, true, false,
                Arg.Any<CancellationToken>())
            .Returns(ExistingEmail);

        var result = await _handler.HandleAsync(ValidEmailRequest(password: "new-password"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _smtpCredentialProtector.Received(1).Protect("new-password");
        await _notificationChannels.Received(1).UpdateAsync(
            2, "My Email Channel", true,
            null, null, null,
            "smtp.example.com", 587, true, "user@example.com", "new-encrypted-password",
            "from@example.com", "to@example.com",
            true, true, false,
            Arg.Any<CancellationToken>());
    }
}
