using Microsoft.Extensions.DependencyInjection;
using StillHere.Application.Features.AuditLog;
using StillHere.Application.Features.Auth;
using StillHere.Application.Features.Dashboard;
using StillHere.Application.Features.DnsProviders;
using StillHere.Application.Features.DomainChecks;
using StillHere.Application.Features.Domains;
using StillHere.Application.Features.Notifications;

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
        services.AddScoped<IDeleteManagedDomainRequestHandler, DeleteManagedDomainRequestHandler>();

        services.AddScoped<IRunScheduledDomainCheckHandler, RunScheduledDomainCheckHandler>();
        services.AddScoped<IRunManualDomainCheckRequestHandler, RunManualDomainCheckRequestHandler>();
        services.AddScoped<IListDueDomainsHandler, ListDueDomainsHandler>();

        services.AddScoped<IGetDashboardSummaryRequestHandler, GetDashboardSummaryRequestHandler>();
        services.AddScoped<IGetAuditLogEntriesRequestHandler, GetAuditLogEntriesRequestHandler>();

        services.AddScoped<ICreateNotificationChannelRequestHandler, CreateNotificationChannelRequestHandler>();
        services.AddScoped<IUpdateNotificationChannelRequestHandler, UpdateNotificationChannelRequestHandler>();
        services.AddScoped<IDeleteNotificationChannelRequestHandler, DeleteNotificationChannelRequestHandler>();
        services.AddSingleton<INotificationSenderRegistry, NotificationSenderRegistry>();

        return services;
    }
}
