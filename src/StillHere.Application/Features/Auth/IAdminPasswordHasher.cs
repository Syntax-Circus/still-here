namespace StillHere.Application.Features.Auth;

public interface IAdminPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
