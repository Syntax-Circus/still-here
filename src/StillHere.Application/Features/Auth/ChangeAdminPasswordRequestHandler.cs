using System.Globalization;
using SyntaxCircus.Common;

namespace StillHere.Application.Features.Auth;

public sealed record ChangeAdminPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

public interface IChangeAdminPasswordRequestHandler
{
    Task<Result> HandleAsync(ChangeAdminPasswordRequest request, CancellationToken cancellationToken);
}

public sealed class ChangeAdminPasswordRequestHandler(
    IAdminUserRepository adminUsers,
    IAdminPasswordHasher passwordHasher,
    ICurrentUserService currentUser) : IChangeAdminPasswordRequestHandler
{
    private const int MinimumPasswordLength = 8;

    public async Task<Result> HandleAsync(ChangeAdminPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated
            || currentUser.UserId is not { } userIdText
            || !int.TryParse(userIdText, CultureInfo.InvariantCulture, out var userId))
        {
            return Result.Failure(new ResultError(
                "authentication-required",
                "Authentication is required.",
                ResultErrorKind.Unauthenticated));
        }

        var credentials = await adminUsers.FindByIdAsync(userId, cancellationToken);
        if (credentials is null || !passwordHasher.Verify(credentials.PasswordHash, request.CurrentPassword))
        {
            return Result.Failure(new ResultError(
                "current-password-incorrect",
                "The current password is incorrect.",
                ResultErrorKind.Validation,
                nameof(request.CurrentPassword)));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < MinimumPasswordLength)
        {
            return Result.Failure(new ResultError(
                "password-too-short",
                $"Password must be at least {MinimumPasswordLength} characters.",
                ResultErrorKind.Validation,
                nameof(request.NewPassword)));
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return Result.Failure(new ResultError(
                "password-mismatch",
                "Passwords do not match.",
                ResultErrorKind.Validation,
                nameof(request.ConfirmNewPassword)));
        }

        var newHash = passwordHasher.Hash(request.NewPassword);
        await adminUsers.UpdatePasswordAsync(userId, newHash, cancellationToken);

        return Result.Success();
    }
}
