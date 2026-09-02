namespace StillHere.Application.Security;

public interface ICredentialProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
