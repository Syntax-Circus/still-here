using Microsoft.Extensions.DependencyInjection;
using StillHere.Application.Features.Auth;
using StillHere.Application.Features.DnsProviders;

namespace StillHere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreateInitialAdminRequestHandler, CreateInitialAdminRequestHandler>();
        services.AddScoped<IAuthenticateAdminRequestHandler, AuthenticateAdminRequestHandler>();
        services.AddScoped<IChangeAdminPasswordRequestHandler, ChangeAdminPasswordRequestHandler>();

        services.AddSingleton<IDnsProviderRegistry, DnsProviderRegistry>();

        return services;
    }
}
