using StillHere.Application.Features.Auth;

namespace StillHere.Web.Features.Auth;

public static class FirstRunGateMiddleware
{
    private static readonly string[] ExemptPrefixes = ["/healthz", "/_framework", "/_blazor", "/favicon"];

    // Static assets (compiled CSS, scoped-CSS bundles, JS interop files, etc.) are served by
    // MapStaticAssets with no authorization requirement of their own -- redirecting them to /setup
    // pre-admin breaks the /setup and /login pages' own styling/scripts, since their <link>/<script>
    // requests get redirected to the /setup HTML document instead of the actual asset.
    private static readonly string[] ExemptStaticAssetExtensions =
        [".css", ".js", ".map", ".ico", ".png", ".svg", ".woff", ".woff2"];

    public static IApplicationBuilder UseFirstRunGate(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            if (Array.Exists(ExemptPrefixes, prefix => path.StartsWithSegments(prefix))
                || Array.Exists(ExemptStaticAssetExtensions, ext => path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
            {
                await next(context);
                return;
            }

            var adminUsers = context.RequestServices.GetRequiredService<IAdminUserRepository>();
            var adminExists = await adminUsers.AnyExistsAsync(context.RequestAborted);

            if (!adminExists && !path.StartsWithSegments("/setup"))
            {
                context.Response.Redirect("/setup");
                return;
            }

            if (adminExists && path.StartsWithSegments("/setup"))
            {
                context.Response.Redirect("/login");
                return;
            }

            await next(context);
        });
}
