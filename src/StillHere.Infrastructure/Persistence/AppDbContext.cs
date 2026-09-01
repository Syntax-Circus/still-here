using Microsoft.EntityFrameworkCore;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    internal DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    internal DbSet<DnsProviderCredential> DnsProviderCredentials => Set<DnsProviderCredential>();

    internal DbSet<ManagedDomain> ManagedDomains => Set<ManagedDomain>();

    internal DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    internal DbSet<GlobalSettings> GlobalSettings => Set<GlobalSettings>();

    internal DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<ManagedDomain>(entity =>
        {
            entity.Property(e => e.LastStatus).HasConversion<string>();
            entity
                .HasOne(e => e.ProviderCredential)
                .WithMany(c => c.ManagedDomains)
                .HasForeignKey(e => e.ProviderCredentialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.Property(e => e.EventType).HasConversion<string>();
            entity
                .HasOne(e => e.ManagedDomain)
                .WithMany()
                .HasForeignKey(e => e.ManagedDomainId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>();
        });

        modelBuilder.Entity<Entities.GlobalSettings>(entity =>
        {
            entity.HasData(new Entities.GlobalSettings
            {
                Id = Entities.GlobalSettings.SingletonId,
                DefaultPollingIntervalSeconds = 300,
                IpDetectionMode = "ExternalAndProviderReported",
                ExternalIpCheckServices = """["https://ifconfig.me/ip","https://api.ipify.org","https://icanhazip.com"]""",
                AuditLogRetentionDays = null,
            });
        });
    }
}
