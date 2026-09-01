using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Auth;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Auth;

public sealed class AuthenticateAdminRequestHandlerTests
{
    private readonly IAdminUserRepository _adminUsers = Substitute.For<IAdminUserRepository>();
    private readonly IAdminPasswordHasher _passwordHasher = Substitute.For<IAdminPasswordHasher>();
    private readonly AuthenticateAdminRequestHandler _handler;

    public AuthenticateAdminRequestHandlerTests()
    {
        _handler = new AuthenticateAdminRequestHandler(_adminUsers, _passwordHasher);
    }

    [Fact]
    public async Task HandleAsync_UnknownUsername_ReturnsUnauthenticated()
    {
        _adminUsers.FindByUsernameAsync("admin", Arg.Any<CancellationToken>())
            .Returns((AdminCredentialsDto?)null);

        var result = await _handler.HandleAsync(new AuthenticateAdminRequest("admin", "anything"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Unauthenticated);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsUnauthenticated()
    {
        _adminUsers.FindByUsernameAsync("admin", Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "wrong-password").Returns(false);

        var result = await _handler.HandleAsync(new AuthenticateAdminRequest("admin", "wrong-password"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Unauthenticated);
        await _adminUsers.DidNotReceive().UpdateLastLoginAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsSuccessAndUpdatesLastLogin()
    {
        _adminUsers.FindByUsernameAsync("admin", Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "correcthorsebattery").Returns(true);

        var result = await _handler.HandleAsync(new AuthenticateAdminRequest("admin", "correcthorsebattery"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe("admin");
        await _adminUsers.Received(1).UpdateLastLoginAsync(1, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
