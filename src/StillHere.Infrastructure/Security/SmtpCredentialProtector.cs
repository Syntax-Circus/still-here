using Microsoft.AspNetCore.DataProtection;
using StillHere.Application.Security;

namespace StillHere.Infrastructure.Security;

/// <summary>
/// Backed by ASP.NET Core Data Protection (already registered by the host for the auth cookie),
/// not SyntaxCircus.Credentials -- that package is a desktop OS credential vault (Windows
/// Credential Manager / macOS Keychain / a Linux secret-tool D-Bus service), not a server-side
/// database-column encryption library. See docs/architecture/04-DECISION-LOG.md Decision 4 and Decision 7.
/// </summary>
internal sealed class SmtpCredentialProtector : ISmtpCredentialProtector
{
    private const string Purpose = "StillHere.SmtpCredentials";

    private readonly IDataProtector _protector;

    public SmtpCredentialProtector(IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
