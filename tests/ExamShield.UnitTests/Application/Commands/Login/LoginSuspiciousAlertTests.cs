using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Login;

public sealed class LoginSuspiciousAlertTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();
    private readonly LoginCommandHandler _sut;

    public LoginSuspiciousAlertTests() =>
        _sut = new LoginCommandHandler(_users, _hasher, _jwt, _refreshTokens, _security, _auditLog,
            alertService: _alerts);

    [Fact]
    public async Task Handle_WhenUserLocksOut_SendsSuspiciousLoginAlert()
    {
        // Simulate user who will lock out on this attempt (FailedLoginAttempts = MaxAttempts-1)
        var user = User.Create(new Email("victim@exam.io"), "$2a$04$hash", UserRole.Invigilator);
        for (var i = 0; i < LoginCommandHandler.MaxFailedAttempts - 1; i++)
            user.RecordFailedLogin(LoginCommandHandler.MaxFailedAttempts, LoginCommandHandler.LockoutDuration);

        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), user.PasswordHash).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.Handle(new LoginCommand("victim@exam.io", "wrong"), default));

        await _alerts.Received(1).SendAsync(
            AlertType.SuspiciousLogin,
            Arg.Is<string>(m => m.Contains("victim@exam.io")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSingleFailure_DoesNotSendSuspiciousAlert()
    {
        var user = User.Create(new Email("user@exam.io"), "$2a$04$hash", UserRole.Invigilator);
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), user.PasswordHash).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.Handle(new LoginCommand("user@exam.io", "wrong"), default));

        await _alerts.DidNotReceive().SendAsync(
            AlertType.SuspiciousLogin, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
