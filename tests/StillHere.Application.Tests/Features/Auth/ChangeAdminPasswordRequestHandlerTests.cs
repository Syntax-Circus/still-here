using NSubstitute;
using Shouldly;
using StillHere.Application.Features.Auth;
using SyntaxCircus.Common;
using Xunit;

namespace StillHere.Application.Tests.Features.Auth;

public sealed class ChangeAdminPasswordRequestHandlerTests
{
    private readonly IAdminUserRepository _adminUsers = Substitute.For<IAdminUserRepository>();
    private readonly IAdminPasswordHasher _passwordHasher = Substitute.For<IAdminPasswordHasher>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ChangeAdminPasswordRequestHandler _handler;

    public ChangeAdminPasswordRequestHandlerTests()
    {
        _handler = new ChangeAdminPasswordRequestHandler(_adminUsers, _passwordHasher, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_NotAuthenticated_ReturnsUnauthenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);

        var result = await _handler.HandleAsync(
            new ChangeAdminPasswordRequest("old", "newpassword1", "newpassword1"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Unauthenticated);
    }

    [Fact]
    public async Task HandleAsync_WrongCurrentPassword_ReturnsValidationError()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("1");
        _adminUsers.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "wrong-current").Returns(false);

        var result = await _handler.HandleAsync(
            new ChangeAdminPasswordRequest("wrong-current", "newpassword1", "newpassword1"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Kind.ShouldBe(ResultErrorKind.Validation);
        result.Errors[0].Target.ShouldBe("CurrentPassword");
    }

    [Fact]
    public async Task HandleAsync_NewPasswordTooShort_ReturnsValidationError()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("1");
        _adminUsers.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "correct-current").Returns(true);

        var result = await _handler.HandleAsync(
            new ChangeAdminPasswordRequest("correct-current", "short", "short"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("NewPassword");
    }

    [Fact]
    public async Task HandleAsync_NewPasswordMismatch_ReturnsValidationError()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("1");
        _adminUsers.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "correct-current").Returns(true);

        var result = await _handler.HandleAsync(
            new ChangeAdminPasswordRequest("correct-current", "newpassword1", "different-password"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Target.ShouldBe("ConfirmNewPassword");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesPasswordAndReturnsSuccess()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("1");
        _adminUsers.FindByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new AdminCredentialsDto(1, "admin", "stored-hash"));
        _passwordHasher.Verify("stored-hash", "correct-current").Returns(true);
        _passwordHasher.Hash("newpassword1").Returns("new-hash");

        var result = await _handler.HandleAsync(
            new ChangeAdminPasswordRequest("correct-current", "newpassword1", "newpassword1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _adminUsers.Received(1).UpdatePasswordAsync(1, "new-hash", Arg.Any<CancellationToken>());
    }
}
