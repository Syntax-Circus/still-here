namespace StillHere.Application.Features.Auth;

public interface IAdminUserRepository
{
    Task<bool> AnyExistsAsync(CancellationToken cancellationToken);

    Task<AdminCredentialsDto?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<AdminCredentialsDto?> FindByIdAsync(int id, CancellationToken cancellationToken);

    Task<AdminUserDto> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken);

    Task UpdatePasswordAsync(int id, string newPasswordHash, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(int id, DateTime lastLoginAtUtc, CancellationToken cancellationToken);
}
