using Microsoft.Extensions.DependencyInjection;
using StillHere.Application.Features.Auth;

namespace StillHere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreateInitialAdminRequestHandler, CreateInitialAdminRequestHandler>();
        services.AddScoped<IAuthenticateAdminRequestHandler, AuthenticateAdminRequestHandler>();
        services.AddScoped<IChangeAdminPasswordRequestHandler, ChangeAdminPasswordRequestHandler>();

        return services;
    }
}
