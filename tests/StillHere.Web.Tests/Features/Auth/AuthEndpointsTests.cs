using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StillHere.Infrastructure.Persistence;
using Xunit;

namespace StillHere.Web.Tests.Features.Auth;

public sealed partial class AuthEndpointsTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stillhere-webtest-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder
                // UseSetting (not ConfigureAppConfiguration) is required here: Program.cs reads
                // ConnectionStrings:Default eagerly while still building WebApplicationBuilder
                // (inside AddInfrastructure), before a later-layered ConfigureAppConfiguration
                // provider would be visible to that eager read.
                .UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}")
                .UseSetting("DataProtection:KeysPath", Path.Combine(Path.GetTempPath(), $"stillhere-webtest-keys-{Guid.NewGuid():N}"))
                .UseSetting("Logging:FilePath", Path.Combine(Path.GetTempPath(), $"stillhere-webtest-logs-{Guid.NewGuid():N}", "log-.txt"))
                // The scheduler's first tick fires immediately on host start (no initial delay),
                // racing this class's own InitializeAsync() migration against the same fresh
                // SQLite file. The tick's own defensive MigrateAsync() call means this can't fail
                // a test outright, but a long interval caps the race to at most one tick for this
                // whole test class's lifetime instead of one every 30s.
                .UseSetting("Scheduler:TickIntervalSeconds", "3600"));
    }

    public async ValueTask InitializeAsync()
    {
        // Program.cs's own startup migration runs after WebApplicationBuilder.Build(), which
        // WebApplicationFactory never reaches (it intercepts the host right after Build()) --
        // so the schema has to be created explicitly here instead.
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Get_StaticCssAsset_NoAdmin_IsNotRedirectedToSetup()
    {
        using var client = CreateClient();

        var setupHtml = await client.GetStringAsync("/setup", TestContext.Current.CancellationToken);
        var cssHref = CssHrefPattern().Match(setupHtml).Groups[1].Value;

        var response = await client.GetAsync(cssHref, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Root_NoAdmin_RedirectsTowardSetup()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/setup");
    }

    [Fact]
    public async Task Post_Setup_CreatesAdmin_SignsIn_AndRedirectsHome()
    {
        using var client = CreateClient();

        var response = await SubmitSetupAsync(client, "admin", "correcthorsebattery");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(c => c.StartsWith("stillhere.auth=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_Setup_OnceAdminExists_RedirectsToLogin()
    {
        using var client = CreateClient();
        await SubmitSetupAsync(client, "admin", "correcthorsebattery");

        var response = await client.GetAsync("/setup", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/login");
    }

    [Fact]
    public async Task Get_Root_Unauthenticated_OnceAdminExists_RedirectsToLogin()
    {
        using var setupClient = CreateClient();
        await SubmitSetupAsync(setupClient, "admin", "correcthorsebattery");

        using var anonymousClient = CreateClient();
        var response = await anonymousClient.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/login");
    }

    [Theory]
    [InlineData("/audit-log")]
    [InlineData("/domains/1/history")]
    public async Task Get_Phase07Routes_Unauthenticated_OnceAdminExists_RedirectsToLogin(string path)
    {
        using var setupClient = CreateClient();
        await SubmitSetupAsync(setupClient, "admin", "correcthorsebattery");

        using var anonymousClient = CreateClient();
        var response = await anonymousClient.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/login");
    }

    [Fact]
    public async Task Post_Login_WrongPassword_RedirectsBackWithError()
    {
        using var client = CreateClient();
        await SubmitSetupAsync(client, "admin", "correcthorsebattery");

        using var loginClient = CreateClient();
        var response = await SubmitLoginAsync(loginClient, "admin", "wrong-password");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/login?error=");
        response.Headers.TryGetValues("Set-Cookie", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Post_Login_ValidCredentials_SignsInAndGrantsAccess()
    {
        using var setupClient = CreateClient();
        await SubmitSetupAsync(setupClient, "admin", "correcthorsebattery");

        using var loginClient = CreateClient();
        var loginResponse = await SubmitLoginAsync(loginClient, "admin", "correcthorsebattery");

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        loginResponse.Headers.Location!.ToString().ShouldBe("/");

        var homeResponse = await loginClient.GetAsync("/", TestContext.Current.CancellationToken);
        homeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> SubmitSetupAsync(HttpClient client, string username, string password)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/setup");

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["username"] = username,
            ["password"] = password,
            ["confirmPassword"] = password,
        };

        return await client.PostAsync("/setup", new FormUrlEncodedContent(form));
    }

    private static async Task<HttpResponseMessage> SubmitLoginAsync(HttpClient client, string username, string password)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/login");

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["username"] = username,
            ["password"] = password,
            ["returnUrl"] = "/",
        };

        return await client.PostAsync("/login", new FormUrlEncodedContent(form));
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenPattern().Match(html);
        match.Success.ShouldBeTrue($"Antiforgery token not found in response from {path}.");
        return match.Groups[1].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"\\s+value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenPattern();

    [GeneratedRegex("<link rel=\"stylesheet\" href=\"([^\"]+\\.css)\"")]
    private static partial Regex CssHrefPattern();
}
