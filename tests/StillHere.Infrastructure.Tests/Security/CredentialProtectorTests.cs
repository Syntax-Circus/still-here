using Microsoft.AspNetCore.DataProtection;
using Shouldly;
using StillHere.Infrastructure.Security;
using Xunit;

namespace StillHere.Infrastructure.Tests.Security;

public sealed class CredentialProtectorTests : IDisposable
{
    private readonly string _keysDirectory = Path.Combine(Path.GetTempPath(), $"stillhere-test-keys-{Guid.NewGuid():N}");
    private readonly CredentialProtector _protector;

    public CredentialProtectorTests()
    {
        var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(_keysDirectory));
        _protector = new CredentialProtector(dataProtectionProvider);
    }

    [Fact]
    public void Protect_ProducesOutputDifferentFromInput()
    {
        var protectedValue = _protector.Protect("{\"Password\":\"ddns-password\"}");

        protectedValue.ShouldNotBe("{\"Password\":\"ddns-password\"}");
        protectedValue.ShouldNotContain("ddns-password");
    }

    [Fact]
    public void Unprotect_OfProtectedValue_RoundTrips()
    {
        const string plaintext = "{\"Password\":\"ddns-password\"}";

        var protectedValue = _protector.Protect(plaintext);
        var roundTripped = _protector.Unprotect(protectedValue);

        roundTripped.ShouldBe(plaintext);
    }

    public void Dispose()
    {
        if (Directory.Exists(_keysDirectory))
        {
            Directory.Delete(_keysDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
