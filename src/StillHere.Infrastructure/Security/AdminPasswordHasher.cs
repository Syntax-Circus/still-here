using Microsoft.AspNetCore.Identity;
using StillHere.Application.Features.Auth;

namespace StillHere.Infrastructure.Security;

internal sealed class AdminPasswordHasher : IAdminPasswordHasher
{
    private const string HashPurpose = "StillHere.AdminUser.Password";

    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashPurpose, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(HashPurpose, hash, password) != PasswordVerificationResult.Failed;
}
