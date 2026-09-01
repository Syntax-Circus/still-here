using System.Net;
using System.Text;
using Shouldly;
using StillHere.Application.Features.DnsProviders;
using StillHere.Infrastructure.DnsProviders;
using Xunit;

namespace StillHere.Infrastructure.Tests.DnsProviders;

public sealed class NamecheapDnsProviderTests
{
    private static readonly IReadOnlyDictionary<string, string> ValidSecrets =
        new Dictionary<string, string> { ["Password"] = "ddns-password" };

    private const string SuccessXml = """
        <?xml version="1.0"?>
        <interface-response>
          <Command>SETDNSHOST</Command>
          <Language>eng</Language>
          <IP>1.2.3.4</IP>
          <ErrCount>0</ErrCount>
          <errors />
          <ResponseCount>0</ResponseCount>
          <responses />
          <Done>true</Done>
          <debug><![CDATA[]]></debug>
        </interface-response>
        """;

    private const string FalseUtf16DeclarationXml = """
        <?xml version="1.0" encoding="utf-16"?>
        <interface-response>
          <Command>SETDNSHOST</Command>
          <Language>eng</Language>
          <IP>5.6.7.8</IP>
          <ErrCount>0</ErrCount>
          <errors />
          <ResponseCount>0</ResponseCount>
          <responses />
          <Done>true</Done>
          <debug><![CDATA[]]></debug>
        </interface-response>
        """;

    private const string ErrorXml = """
        <?xml version="1.0"?>
        <interface-response>
          <Command>SETDNSHOST</Command>
          <Language>eng</Language>
          <ErrCount>1</ErrCount>
          <errors><Err1>Domain name not found</Err1></errors>
          <ResponseCount>0</ResponseCount>
          <responses />
          <Done>false</Done>
          <debug><![CDATA[]]></debug>
        </interface-response>
        """;

    [Fact]
    public async Task UpdateAsync_SuccessResponse_ReturnsSuccessWithReportedIp()
    {
        var handler = new StubHttpMessageHandler(_ => TextResponse(SuccessXml));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.ProviderReportedIp.ShouldBe("1.2.3.4");
    }

    [Fact]
    public async Task UpdateAsync_ResponseFalselyDeclaresUtf16ButIsUtf8_ParsesCorrectly()
    {
        // Real Namecheap bug: the XML declaration always claims utf-16 while the bytes are UTF-8.
        var handler = new StubHttpMessageHandler(_ => TextResponse(FalseUtf16DeclarationXml));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.ProviderReportedIp.ShouldBe("5.6.7.8");
    }

    [Fact]
    public async Task UpdateAsync_ErrorResponse_ReturnsFailureWithErrorMessage()
    {
        var handler = new StubHttpMessageHandler(_ => TextResponse(ErrorXml));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Domain name not found");
    }

    [Fact]
    public async Task UpdateAsync_ErrorResponseAlwaysReturnsHttp200_StillDetectedAsFailure()
    {
        // Namecheap always returns HTTP 200, even for API-level errors -- confirm the provider
        // doesn't treat the 200 status code itself as success.
        var handler = new StubHttpMessageHandler(_ => TextResponse(ErrorXml, HttpStatusCode.OK));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_MissingPassword_ReturnsFailureWithoutMakingHttpCall()
    {
        var handler = new StubHttpMessageHandler(_ => TextResponse(SuccessXml));
        var provider = CreateProvider(handler);
        var request = new DnsUpdateRequest("example.com", "@", new Dictionary<string, string>(), "1.2.3.4");

        var result = await provider.UpdateAsync(request, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        handler.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_MalformedResponse_ReturnsFailureWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => TextResponse("not xml at all"));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NetworkFailure_ReturnsFailureWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("simulated network failure"));
        var provider = CreateProvider(handler);

        var result = await provider.UpdateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    private static NamecheapDnsProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dynamicdns.park-your-domain.com/"),
        };

        return new NamecheapDnsProvider(httpClient);
    }

    private static DnsUpdateRequest CreateRequest() =>
        new("example.com", "@", ValidSecrets, "1.2.3.4");

    private static HttpResponseMessage TextResponse(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body)),
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responder(request));
        }
    }
}
