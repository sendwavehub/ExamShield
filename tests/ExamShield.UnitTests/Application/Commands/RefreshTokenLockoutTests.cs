using ExamShield.Application.Commands.Login;
using ExamShield.Application.Commands.Refresh;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class RefreshTokenLockoutTests
{
    private readonly IRefreshTokenRepository  _tokens   = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository          _users    = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService         _jwt      = Substitute.For<IJwtTokenService>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository      _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenLockoutTests() =>
        _sut = new RefreshTokenCommandHandler(_tokens, _users, _jwt, _security, _auditLog);

    private static RefreshToken ActiveToken(UserId userId)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("raw-token")));
        return RefreshToken.Create(userId, hash, expiryDays: 7);
    }

    [Fact]
    public async Task Handle_WhenUserIsLockedOut_ThrowsInvalidCredentialsException()
    {
        var user = User.Create(new Email("locked@test.com"), "hash", UserRole.Operator);
        for (var i = 0; i < LoginCommandHandler.MaxFailedAttempts; i++)
            user.RecordFailedLogin(LoginCommandHandler.MaxFailedAttempts, LoginCommandHandler.LockoutDuration);
        Assert.True(user.IsLockedOut);

        var token = ActiveToken(user.Id);
        _tokens.FindByHashAsync(Arg.Any<string>(), default).Returns(token);
        _users.GetByIdAsync(user.Id, default).Returns(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.Handle(new RefreshTokenCommand("raw-token"), default));
    }

    [Fact]
    public async Task Handle_WhenUserIsActiveAndNotLocked_ReturnsNewTokens()
    {
        var user = User.Create(new Email("ok@test.com"), "hash", UserRole.Operator);
        var token = ActiveToken(user.Id);
        _tokens.FindByHashAsync(Arg.Any<string>(), default).Returns(token);
        _users.GetByIdAsync(user.Id, default).Returns(user);
        _jwt.Generate(Arg.Any<User>()).Returns("new-jwt");

        var result = await _sut.Handle(new RefreshTokenCommand("raw-token"), default);

        Assert.Equal("new-jwt", result.Token);
    }
}
