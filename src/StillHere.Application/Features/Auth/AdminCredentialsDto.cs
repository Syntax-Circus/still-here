namespace StillHere.Application.Features.Auth;

/// <summary>Internal-use-only shape carrying the password hash for verification. Never returned to the UI.</summary>
public sealed record AdminCredentialsDto(int Id, string Username, string PasswordHash);
