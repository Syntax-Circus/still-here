using Shouldly;
using StillHere.Infrastructure.Security;
using Xunit;

namespace StillHere.Infrastructure.Tests.Security;

public sealed class AdminPasswordHasherTests
{
    private readonly AdminPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesAVerifiableHash()
    {
        var hash = _hasher.Hash("correcthorsebattery");

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotBe("correcthorsebattery");
        _hasher.Verify(hash, "correcthorsebattery").ShouldBeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("correcthorsebattery");

        _hasher.Verify(hash, "wrong-password").ShouldBeFalse();
    }
}
