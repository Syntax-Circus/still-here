using Microsoft.EntityFrameworkCore;
using StillHere.Application.Features.Auth;
using StillHere.Infrastructure.Persistence.Entities;

namespace StillHere.Infrastructure.Persistence.Repositories;

internal sealed class AdminUserRepository(AppDbContext db) : IAdminUserRepository
{
    public Task<bool> AnyExistsAsync(CancellationToken cancellationToken) =>
        db.AdminUsers.AnyAsync(cancellationToken);

    public async Task<AdminCredentialsDto?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        return user is null ? null : ToCredentialsDto(user);
    }

    public async Task<AdminCredentialsDto?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? null : ToCredentialsDto(user);
    }

    public async Task<AdminUserDto> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken)
    {
        var user = new AdminUser
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.AdminUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new AdminUserDto(user.Id, user.Username, user.CreatedAtUtc);
    }

    public async Task UpdatePasswordAsync(int id, string newPasswordHash, CancellationToken cancellationToken)
    {
        var user = await db.AdminUsers.FirstAsync(u => u.Id == id, cancellationToken);
        user.PasswordHash = newPasswordHash;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastLoginAsync(int id, DateTime lastLoginAtUtc, CancellationToken cancellationToken)
    {
        var user = await db.AdminUsers.FirstAsync(u => u.Id == id, cancellationToken);
        user.LastLoginAtUtc = lastLoginAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static AdminCredentialsDto ToCredentialsDto(AdminUser user) =>
        new(user.Id, user.Username, user.PasswordHash);
}
