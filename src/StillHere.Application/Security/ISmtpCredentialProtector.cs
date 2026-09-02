namespace StillHere.Application.Security;

public interface ISmtpCredentialProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
