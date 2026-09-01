using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StillHere.Application.Features.Auth;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using StillHere.Infrastructure.Security;

namespace StillHere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing required connection string 'ConnectionStrings:Default'.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddSingleton<IAdminPasswordHasher, AdminPasswordHasher>();

        return services;
    }
}
