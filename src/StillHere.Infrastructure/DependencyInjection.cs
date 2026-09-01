using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StillHere.Application.Features.AuditLog;
using StillHere.Application.Features.Auth;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.Domains;
using StillHere.Application.Features.Notifications;
using StillHere.Application.Features.Settings;
using StillHere.Application.IpDetection;
using StillHere.Application.Security;
using StillHere.Infrastructure.DnsProviders;
using StillHere.Infrastructure.IpDetection;
using StillHere.Infrastructure.Notifications;
using StillHere.Infrastructure.Persistence;
using StillHere.Infrastructure.Persistence.Repositories;
using StillHere.Infrastructure.Scheduling;
using StillHere.Infrastructure.Security;
using SyntaxCircus.Http.Resilience;

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

        services.AddScoped<IManagedDomainRepository, ManagedDomainRepository>();
        services.AddScoped<INotificationChannelRepository, NotificationChannelRepository>();
        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddSingleton<ISmtpCredentialProtector, SmtpCredentialProtector>();
        services.AddScoped<IGlobalSettingsReader, GlobalSettingsReader>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddResilientHttpClient(
            "namecheap-ddns",
            client =>
            {
                client.BaseAddress = new Uri("https://dynamicdns.park-your-domain.com/");
                client.Timeout = TimeSpan.FromSeconds(15);
            },
            retryCount: 3,
            onRetry: (name, attempt, statusCode) =>
                Log.Warning("HTTP retry {Attempt} for {Client} ({StatusCode})", attempt, name, statusCode),
            onBreak: (name, statusCode) =>
                Log.Warning("Circuit opened for {Client} ({StatusCode})", name, statusCode))
            .AddTypedClient<IDnsProvider, NamecheapDnsProvider>();

        services.AddSingleton<IpDetectionCache>();
        services.AddScoped<IIpDetectionService, IpDetectionService>();

        services.AddResilientHttpClient(
            IpDetectionService.IpCheckHttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(5),
            retryCount: 1,
            onRetry: (name, attempt, statusCode) =>
                Log.Warning("HTTP retry {Attempt} for {Client} ({StatusCode})", attempt, name, statusCode),
            onBreak: (name, statusCode) =>
                Log.Warning("Circuit opened for {Client} ({StatusCode})", name, statusCode));

        services.AddResilientHttpClient(
            "notification-webhook",
            client => client.Timeout = TimeSpan.FromSeconds(10),
            retryCount: 2,
            onRetry: (name, attempt, statusCode) =>
                Log.Warning("HTTP retry {Attempt} for {Client} ({StatusCode})", attempt, name, statusCode),
            onBreak: (name, statusCode) =>
                Log.Warning("Circuit opened for {Client} ({StatusCode})", name, statusCode))
            .AddTypedClient<INotificationSender, WebhookNotificationSender>();

        // Transient (not Scoped) -- INotificationSenderRegistry is a Singleton (mirrors
        // IDnsProviderRegistry) and consumes IEnumerable<INotificationSender> at construction, so
        // every INotificationSender implementation must be safe to capture that way.
        // WebhookNotificationSender is already Transient via AddTypedClient's own convention;
        // EmailNotificationSender holds no scoped state either (its dependencies are singletons),
        // so it matches here rather than being the odd one out.
        services.AddTransient<INotificationSender, EmailNotificationSender>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Makes ILogger<T> resolvable even from a bare ServiceCollection in DI tests -- real
        // Program.cs's WebApplicationBuilder already provides this implicitly. First place in the
        // codebase using ILogger<T> DI instead of static Serilog.Log, forced by
        // PeriodicBackgroundService's own constructor signature.
        services.AddLogging();

        var schedulerIntervalSeconds = configuration.GetValue<int?>("Scheduler:TickIntervalSeconds")
            ?? DomainCheckScheduler.DefaultTickIntervalSeconds;

        services.AddHostedService(sp => new DomainCheckScheduler(
            sp.GetRequiredService<IServiceScopeFactory>(),
            TimeSpan.FromSeconds(schedulerIntervalSeconds),
            sp.GetRequiredService<ILogger<DomainCheckScheduler>>()));

        return services;
    }
}
