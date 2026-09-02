using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Auth;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Auth;

public sealed class CreateInitialAdminRequestHandlerTests
{
    private readonly IAdminUserRepository _adminUsers = Substitute.For<IAdminUserRepository>();
    private readonly IAdminPasswordHasher _passwordHasher = Substitute.For<IAdminPasswordHasher>();
    private readonly CreateInitialAdminRequestHandler _handler;

    public CreateInitialAdminRequestHandlerTests()
    {
        _handler = new CreateInitialAdminRequestHandler(_adminUsers, _passwordHasher);
    }

    [Fact]
    public async Task HandleAsync_AdminAlreadyExists_ReturnsConflict()
    {
        _adminUsers.AnyExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(
            new CreateInitialAdminRequest("admin", "correcthorsebattery", "correcthorsebattery"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Conflict);
    }

    [Fact]
    public async Task HandleAsync_EmptyUsername_ReturnsValidationError()
    {
        _adminUsers.AnyExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(
            new CreateInitialAdminRequest("", "correcthorsebattery", "correcthorsebattery"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Target == "Username");
    }

    [Fact]
    public async Task HandleAsync_PasswordTooShort_ReturnsValidationError()
    {
        _adminUsers.AnyExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(
            new CreateInitialAdminRequest("admin", "short", "short"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Target == "Password");
    }

    [Fact]
    public async Task HandleAsync_PasswordMismatch_ReturnsValidationError()
    {
        _adminUsers.AnyExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(
            new CreateInitialAdminRequest("admin", "correcthorsebattery", "somethingelse"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Target == "ConfirmPassword");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_HashesPasswordAndCreatesAdmin()
    {
        _adminUsers.AnyExistsAsync(Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("correcthorsebattery").Returns("hashed-password");
        _adminUsers.CreateAsync("admin", "hashed-password", Arg.Any<CancellationToken>())
            .Returns(new AdminUserDto(1, "admin", DateTime.UtcNow));

        var result = await _handler.HandleAsync(
            new CreateInitialAdminRequest("admin", "correcthorsebattery", "correcthorsebattery"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe("admin");
        await _adminUsers.Received(1).CreateAsync("admin", "hashed-password", Arg.Any<CancellationToken>());
    }
}
