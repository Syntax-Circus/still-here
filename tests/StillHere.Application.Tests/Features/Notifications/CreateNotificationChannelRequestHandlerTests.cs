using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Application.Security;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class CreateNotificationChannelRequestHandlerTests
{
    private readonly INotificationChannelRepository _notificationChannels = Substitute.For<INotificationChannelRepository>();
    private readonly ISmtpCredentialProtector _smtpCredentialProtector = Substitute.For<ISmtpCredentialProtector>();
    private readonly CreateNotificationChannelRequestHandler _handler;

    public CreateNotificationChannelRequestHandlerTests()
    {
        _handler = new CreateNotificationChannelRequestHandler(_notificationChannels, _smtpCredentialProtector);
    }

    private static CreateNotificationChannelRequest ValidWebhookRequest() => new(
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

    private static CreateNotificationChannelRequest ValidEmailRequest(string? password = "smtp-password") => new(
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

    private static NotificationChannelDto SampleDto(NotificationChannelType type = NotificationChannelType.Webhook) => new(
        1, type, "Name", true, "https://example.com/hook", null, "POST",
        null, null, false, null, null, null, null, true, false, false);

    [Fact]
    public async Task HandleAsync_MissingName_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { Name = "" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("name-required");
        result.Errors[0].Target.ShouldBe("Name");
    }

    [Fact]
    public async Task HandleAsync_WebhookMissingUrl_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { Url = "" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("url-required");
        result.Errors[0].Target.ShouldBe("Url");
    }

    [Fact]
    public async Task HandleAsync_WebhookBlankHttpMethod_DefaultsToPost()
    {
        _notificationChannels.CreateAsync(
                NotificationChannelType.Webhook, "My Webhook", true,
                "https://example.com/hook", null, "POST",
                null, null, false, null, null,
                null, null,
                true, false, false,
                Arg.Any<CancellationToken>())
            .Returns(SampleDto());

        var result = await _handler.HandleAsync(
            ValidWebhookRequest() with { HttpMethod = "" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _notificationChannels.Received(1).CreateAsync(
            NotificationChannelType.Webhook, "My Webhook", true,
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
    public async Task HandleAsync_EmailWithPassword_EncryptsPasswordViaProtector()
    {
        _smtpCredentialProtector.Protect("smtp-password").Returns("encrypted-password");
        _notificationChannels.CreateAsync(
                Arg.Any<NotificationChannelType>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(SampleDto(NotificationChannelType.Email));

        var result = await _handler.HandleAsync(ValidEmailRequest(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _smtpCredentialProtector.Received(1).Protect("smtp-password");
        await _notificationChannels.Received(1).CreateAsync(
            NotificationChannelType.Email, "My Email Channel", true,
            null, null, null,
            "smtp.example.com", 587, true, "user@example.com", "encrypted-password",
            "from@example.com", "to@example.com",
            true, true, false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailWithBlankPassword_PassesNullEncryptedPassword()
    {
        _notificationChannels.CreateAsync(
                Arg.Any<NotificationChannelType>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(SampleDto(NotificationChannelType.Email));

        var result = await _handler.HandleAsync(ValidEmailRequest(password: ""), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _smtpCredentialProtector.DidNotReceive().Protect(Arg.Any<string>());
        await _notificationChannels.Received(1).CreateAsync(
            NotificationChannelType.Email, "My Email Channel", true,
            null, null, null,
            "smtp.example.com", 587, true, "user@example.com", null,
            "from@example.com", "to@example.com",
            true, true, false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidWebhookRequest_ReturnsRepositoryDto()
    {
        var expected = SampleDto();
        _notificationChannels.CreateAsync(
                Arg.Any<NotificationChannelType>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.HandleAsync(ValidWebhookRequest() with { HttpMethod = "PUT" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }
}
