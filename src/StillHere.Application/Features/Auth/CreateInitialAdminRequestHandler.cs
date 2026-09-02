using SyntaxCircus.Common;

namespace StillHere.Application.Features.Auth;

public sealed record CreateInitialAdminRequest(string Username, string Password, string ConfirmPassword);

public interface ICreateInitialAdminRequestHandler
{
    Task<Result<AdminUserDto>> HandleAsync(CreateInitialAdminRequest request, CancellationToken cancellationToken);
}

public sealed class CreateInitialAdminRequestHandler(
    IAdminUserRepository adminUsers,
    IAdminPasswordHasher passwordHasher) : ICreateInitialAdminRequestHandler
{
    private const int MinimumPasswordLength = 8;

    public async Task<Result<AdminUserDto>> HandleAsync(
        CreateInitialAdminRequest request,
        CancellationToken cancellationToken)
    {
        if (await adminUsers.AnyExistsAsync(cancellationToken))
        {
            return Result<AdminUserDto>.Failure(new ResultError(
                "admin-already-exists",
                "An admin account already exists.",
                ResultErrorKind.Conflict));
        }

        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result<AdminUserDto>.Failure(validationErrors[0], [.. validationErrors.Skip(1)]);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var created = await adminUsers.CreateAsync(request.Username, passwordHash, cancellationToken);

        return Result<AdminUserDto>.Success(created);
    }

    private static List<ResultError> Validate(CreateInitialAdminRequest request)
    {
        var errors = new List<ResultError>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add(new ResultError(
                "username-required",
                "A username is required.",
                ResultErrorKind.Validation,
                nameof(request.Username)));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinimumPasswordLength)
        {
            errors.Add(new ResultError(
                "password-too-short",
                $"Password must be at least {MinimumPasswordLength} characters.",
                ResultErrorKind.Validation,
                nameof(request.Password)));
        }

        if (request.Password != request.ConfirmPassword)
        {
            errors.Add(new ResultError(
                "password-mismatch",
                "Passwords do not match.",
                ResultErrorKind.Validation,
                nameof(request.ConfirmPassword)));
        }

        return errors;
    }
}
