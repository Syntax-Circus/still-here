using System.Net;
using Shouldly;
using StillHere.Application.Features.Notifications;
using StillHere.Infrastructure.Notifications;
using Xunit;

namespace StillHere.Infrastructure.Tests.Notifications;

public sealed class WebhookNotificationSenderTests
{
    private static readonly NotificationEventContext Context =
        new("example.com", "1.2.3.4", "5.6.7.8", "Success", "IP changed");

    [Fact]
    public async Task SendAsync_SuccessResponse_UsesChannelMethodUrlAndSubstitutedBody()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(handler);
        var channel = CreateChannel(url: "https://hooks.example.com/notify", httpMethod: "PUT", bodyTemplate: "{domain} went {status}: {message} ({oldIp} -> {newIp})");

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        capturedRequest.ShouldNotBeNull();
        capturedRequest!.Method.ShouldBe(HttpMethod.Put);
        capturedRequest.RequestUri.ShouldBe(new Uri("https://hooks.example.com/notify"));
        capturedBody.ShouldBe("example.com went Success: IP changed (1.2.3.4 -> 5.6.7.8)");
    }

    [Fact]
    public async Task SendAsync_NoHttpMethodOrBodyTemplate_DefaultsToPostAndDefaultJsonTemplate()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(handler);
        var channel = CreateChannel(url: "https://hooks.example.com/notify", httpMethod: null, bodyTemplate: null);

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        capturedRequest!.Method.ShouldBe(HttpMethod.Post);
        capturedBody.ShouldBe(
            """{"domain":"example.com","oldIp":"1.2.3.4","newIp":"5.6.7.8","status":"Success","message":"IP changed"}""");
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFailedResult()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sender = CreateSender(handler);
        var channel = CreateChannel(url: "https://hooks.example.com/notify", httpMethod: "POST", bodyTemplate: null);

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_HttpRequestExceptionThrown_IsCaughtAndReturnsFailedResultWithoutPropagating()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("simulated network failure"));
        var sender = CreateSender(handler);
        var channel = CreateChannel(url: "https://hooks.example.com/notify", httpMethod: "POST", bodyTemplate: null);

        var result = await sender.SendAsync(channel, Context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    private static WebhookNotificationSender CreateSender(HttpMessageHandler handler) =>
        new(new HttpClient(handler));

    private static NotificationChannelDto CreateChannel(string url, string? httpMethod, string? bodyTemplate) =>
        new(
            Id: 1,
            Type: NotificationChannelType.Webhook,
            Name: "Test Webhook",
            Enabled: true,
            Url: url,
            BodyTemplate: bodyTemplate,
            HttpMethod: httpMethod,
            SmtpHost: null,
            SmtpPort: null,
            UseSsl: false,
            Username: null,
            EncryptedPassword: null,
            FromAddress: null,
            ToAddresses: null,
            TriggerOnIpChange: true,
            TriggerOnFailure: true,
            TriggerOnSuccess: true);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request);
    }
}
