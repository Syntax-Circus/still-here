using SyntaxCircus.Common;

namespace StillHere.Application.Features.Auth;

public sealed record AuthenticateAdminRequest(string Username, string Password);

public interface IAuthenticateAdminRequestHandler
{
    Task<Result<AuthenticatedAdminDto>> HandleAsync(AuthenticateAdminRequest request, CancellationToken cancellationToken);
}

public sealed class AuthenticateAdminRequestHandler(
    IAdminUserRepository adminUsers,
    IAdminPasswordHasher passwordHasher) : IAuthenticateAdminRequestHandler
{
    public async Task<Result<AuthenticatedAdminDto>> HandleAsync(
        AuthenticateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var credentials = await adminUsers.FindByUsernameAsync(request.Username, cancellationToken);

        // Don't leak whether the username existed -- same error either way.
        if (credentials is null || !passwordHasher.Verify(credentials.PasswordHash, request.Password))
        {
            return Result<AuthenticatedAdminDto>.Failure(new ResultError(
                "invalid-credentials",
                "Invalid username or password.",
                ResultErrorKind.Unauthenticated));
        }

        await adminUsers.UpdateLastLoginAsync(credentials.Id, DateTime.UtcNow, cancellationToken);

        return Result<AuthenticatedAdminDto>.Success(new AuthenticatedAdminDto(credentials.Id, credentials.Username));
    }
}
