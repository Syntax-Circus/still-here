using System.Net;
using System.Text;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shouldly;
using StillHere.Application.Features.DnsProviders;
using StillHere.Infrastructure.DnsProviders;
using Xunit;

namespace StillHere.Infrastructure.Tests.DnsProviders;

public sealed class CloudflareDnsProviderTests
{
    private static readonly IReadOnlyDictionary<string, string> ValidSecrets =
        new Dictionary<string, string> { ["ApiToken"] = "cf-token" };

    private const string ZoneFoundJson = """{"success":true,"errors":[],"result":[{"id":"zone1","name":"example.com"}]}""";
    private const string ZoneMissingJson = """{"success":true,"errors":[],"result":[]}""";
    private const string PatchOkJson = """{"success":true,"errors":[],"result":{"id":"rec1","content":"1.2.3.4"}}""";

    private static string RecordsJson(string content, string id = "rec1") =>
        $$"""{"success":true,"errors":[],"result":[{"id":"{{id}}","type":"A","name":"example.com","content":"{{content}}"}]}""";

    private const string NoRecordsJson = """{"success":true,"errors":[],"result":[]}""";

    [Fact]
    public async Task UpdateAsync_RecordWithStaleIp_PatchesRecordAndReturnsSuccess()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("9.9.9.9"), patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.ProviderReportedIp.ShouldBe("1.2.3.4");
        var patch = handler.Requests.Single(r => r.Method == HttpMethod.Patch);
        patch.PathAndQuery.ShouldBe("/client/v4/zones/zone1/dns_records/rec1");
        patch.Body.ShouldContain("\"content\":\"1.2.3.4\"");
    }

    [Fact]
    public async Task UpdateAsync_RecordAlreadyCurrent_SkipsPatchAndReturnsSuccess()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("1.2.3.4"), patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.ProviderReportedIp.ShouldBe("1.2.3.4");
        handler.Requests.ShouldNotContain(r => r.Method == HttpMethod.Patch);
    }

    [Fact]
    public async Task UpdateAsync_ApexHost_QueriesRecordByDomainName()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("1.2.3.4"), patch: PatchOkJson);

        await CreateProvider(handler).UpdateAsync(CreateRequest(host: "@"), CancellationToken.None);

        handler.Requests.ShouldContain(r => r.PathAndQuery.Contains("/dns_records?") && r.PathAndQuery.Contains("name=example.com") && r.PathAndQuery.Contains("type=A"));
    }

    [Fact]
    public async Task UpdateAsync_SubdomainHost_QueriesRecordByFqdn()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("1.2.3.4"), patch: PatchOkJson);

        await CreateProvider(handler).UpdateAsync(CreateRequest(host: "home"), CancellationToken.None);

        handler.Requests.ShouldContain(r => r.PathAndQuery.Contains("name=home.example.com"));
    }

    [Fact]
    public async Task UpdateAsync_ZoneNotFound_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: ZoneMissingJson, records: NoRecordsJson, patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("zone");
    }

    [Fact]
    public async Task UpdateAsync_RecordNotFound_ReturnsFailureWithoutCreatingRecord()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("record");
        handler.Requests.ShouldNotContain(r => r.Method == HttpMethod.Post || r.Method == HttpMethod.Patch);
    }

    [Fact]
    public async Task UpdateAsync_ApiReportsError_ReturnsFailureWithFirstErrorMessage()
    {
        const string errorJson = """{"success":false,"errors":[{"code":9109,"message":"Invalid access token"}],"result":null}""";
        var handler = new RoutingHandler(zones: errorJson, records: NoRecordsJson, patch: PatchOkJson, zonesStatus: HttpStatusCode.Forbidden);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Invalid access token");
    }

    [Fact]
    public async Task UpdateAsync_PatchReportsError_ReturnsFailure()
    {
        const string errorJson = """{"success":false,"errors":[{"code":1004,"message":"DNS Validation Error"}],"result":null}""";
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("9.9.9.9"), patch: errorJson, patchStatus: HttpStatusCode.BadRequest);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("DNS Validation Error");
    }

    [Fact]
    public async Task UpdateAsync_MissingToken_ReturnsFailureWithoutCallingApi()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("9.9.9.9"), patch: PatchOkJson);
        var request = CreateRequest() with { CredentialSecrets = new Dictionary<string, string>() };

        var result = await CreateProvider(handler).UpdateAsync(request, CancellationToken.None);

        result.Success.ShouldBeFalse();
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_MalformedJson_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: "not json at all", records: NoRecordsJson, patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NetworkFailure_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson, sendException: new HttpRequestException("simulated network failure"));

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_RequestTimesOut_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson, sendException: new TaskCanceledException("simulated timeout", new TimeoutException()));

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("timed out");
    }

    [Fact]
    public async Task UpdateAsync_CircuitOpen_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson, sendException: new BrokenCircuitException());

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("temporarily unavailable");
    }

    [Fact]
    public async Task UpdateAsync_PipelineTimeout_ReturnsFailure()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson, sendException: new TimeoutRejectedException("simulated"));

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("timed out");
    }

    [Fact]
    public async Task UpdateAsync_CallerCancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: NoRecordsJson, patch: PatchOkJson, sendException: new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => CreateProvider(handler).UpdateAsync(CreateRequest(), cts.Token));
    }

    [Fact]
    public async Task UpdateAsync_MultipleZonesMatch_ReturnsFailureWithoutQueryingRecords()
    {
        const string twoZones = """{"success":true,"errors":[],"result":[{"id":"zone1"},{"id":"zone2"}]}""";
        var handler = new RoutingHandler(zones: twoZones, records: RecordsJson("9.9.9.9"), patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("multiple zones");
        handler.Requests.ShouldNotContain(r => r.PathAndQuery.Contains("/dns_records"));
    }

    [Fact]
    public async Task UpdateAsync_MultipleARecordsMatch_ReturnsFailureWithoutPatching()
    {
        const string twoRecords = """{"success":true,"errors":[],"result":[{"id":"rec1","content":"9.9.9.9"},{"id":"rec2","content":"8.8.8.8"}]}""";
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: twoRecords, patch: PatchOkJson);

        var result = await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("2 A records");
        handler.Requests.ShouldNotContain(r => r.Method == HttpMethod.Patch);
    }

    [Fact]
    public async Task UpdateAsync_SendsBearerAuthorizationHeader()
    {
        var handler = new RoutingHandler(zones: ZoneFoundJson, records: RecordsJson("1.2.3.4"), patch: PatchOkJson);

        await CreateProvider(handler).UpdateAsync(CreateRequest(), CancellationToken.None);

        handler.Requests.ShouldAllBe(r => r.Authorization == "Bearer cf-token");
    }

    private static CloudflareDnsProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        };

        return new CloudflareDnsProvider(httpClient);
    }

    private static DnsUpdateRequest CreateRequest(string host = "@") =>
        new("example.com", host, ValidSecrets, "1.2.3.4");

    private sealed record CapturedRequest(HttpMethod Method, string PathAndQuery, string? Authorization, string Body);

    private sealed class RoutingHandler(
        string zones,
        string records,
        string patch,
        HttpStatusCode zonesStatus = HttpStatusCode.OK,
        HttpStatusCode patchStatus = HttpStatusCode.OK,
        Exception? sendException = null) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (sendException is not null)
            {
                throw sendException;
            }

            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var pathAndQuery = request.RequestUri!.PathAndQuery;
            Requests.Add(new CapturedRequest(request.Method, pathAndQuery, request.Headers.Authorization?.ToString(), body));

            if (request.Method == HttpMethod.Patch)
            {
                return Json(patch, patchStatus);
            }

            return pathAndQuery.Contains("/dns_records", StringComparison.Ordinal)
                ? Json(records, HttpStatusCode.OK)
                : Json(zones, zonesStatus);
        }

        private static HttpResponseMessage Json(string body, HttpStatusCode status) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
