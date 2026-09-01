using StillHere.Application.Features.Auth;

namespace StillHere.Web.Features.Auth;

public static class FirstRunGateMiddleware
{
    private static readonly string[] ExemptPrefixes = ["/healthz", "/_framework", "/_blazor", "/favicon"];

    public static IApplicationBuilder UseFirstRunGate(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            if (Array.Exists(ExemptPrefixes, prefix => path.StartsWithSegments(prefix)))
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
