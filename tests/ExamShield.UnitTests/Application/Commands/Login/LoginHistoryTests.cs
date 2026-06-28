using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Login;

public sealed class LoginHistoryTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISecurityEventRepository _security  = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository      _auditLog  = Substitute.For<IAuditLogRepository>();
    private readonly LoginCommandHandler _sut;

    public LoginHistoryTests() =>
        _sut = new LoginCommandHandler(_users, _hasher, _jwt, _refreshTokens, _security, _auditLog);

    private static User MakeUser() =>
        User.Create(new Email("op@examshield.io"), "$2a$04$hash", UserRole.Operator);

    [Fact]
    public async Task Handle_SuccessfulLogin_RecordsLoginSuccessEvent()
    {
        var user = MakeUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);
        _jwt.Generate(user).Returns("jwt");

        await _sut.Handle(
            new LoginCommand("op@examshield.io", "secret", IpAddress: "1.2.3.4"), default);

        await _security.Received(1).AddAsync(
            Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.LoginSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedLogin_RecordsLoginFailedEvent()
    {
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        try
        {
            await _sut.Handle(
                new LoginCommand("x@y.com", "bad", IpAddress: "5.6.7.8"), default);
        }
        catch (InvalidCredentialsException) { /* expected */ }

        await _security.Received(1).AddAsync(
            Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.LoginFailed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_RecordsCorrectIpAddress()
    {
        var user = MakeUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);
        _jwt.Generate(user).Returns("jwt");

        await _sut.Handle(
            new LoginCommand("op@examshield.io", "secret", IpAddress: "192.168.1.1"), default);

        await _security.Received(1).AddAsync(
            Arg.Is<SecurityEvent>(e => e.IpAddress == "192.168.1.1"),
            Arg.Any<CancellationToken>());
    }
}
