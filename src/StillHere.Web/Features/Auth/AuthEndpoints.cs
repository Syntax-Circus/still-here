using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using StillHere.Application.Features.Auth;

namespace StillHere.Web.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/setup", (Delegate)HandleSetupAsync);
        endpoints.MapPost("/login", (Delegate)HandleLoginAsync);
        endpoints.MapPost("/logout", (Delegate)HandleLogoutAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleSetupAsync(
        HttpContext httpContext,
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] string confirmPassword,
        [FromServices] ICreateInitialAdminRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateInitialAdminRequest(username, password, confirmPassword),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Redirect($"/setup?error={Uri.EscapeDataString(result.Errors[0].Message)}");
        }

        await SignInAsync(httpContext, result.Value.Id, result.Value.Username);
        return Results.Redirect("/");
    }

    private static async Task<IResult> HandleLoginAsync(
        HttpContext httpContext,
        [FromForm] string username,
        [FromForm] string password,
        [FromForm(Name = "returnUrl")] string? returnUrl,
        [FromServices] IAuthenticateAdminRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AuthenticateAdminRequest(username, password), cancellationToken);

        if (result.IsFailure)
        {
            var backTo = $"/login?error={Uri.EscapeDataString(result.Errors[0].Message)}";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                backTo += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            return Results.Redirect(backTo);
        }

        await SignInAsync(httpContext, result.Value.Id, result.Value.Username);
        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    private static async Task<IResult> HandleLogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    private static async Task SignInAsync(HttpContext httpContext, int userId, string username)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString(CultureInfo.InvariantCulture)),
            new("name", username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTimeOffset.UtcNow,
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
