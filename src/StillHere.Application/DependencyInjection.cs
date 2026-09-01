using Microsoft.Extensions.DependencyInjection;
using StillHere.Application.Features.Auth;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;

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

        services.AddScoped<IAddManagedDomainRequestHandler, AddManagedDomainRequestHandler>();
        services.AddScoped<IUpdateManagedDomainRequestHandler, UpdateManagedDomainRequestHandler>();

        return services;
    }
}
