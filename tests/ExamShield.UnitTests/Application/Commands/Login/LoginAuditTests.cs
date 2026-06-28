using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Login;

public sealed class LoginAuditTests
{
    private readonly IUserRepository           _users         = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher           _hasher        = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService          _jwt           = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository   _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISecurityEventRepository  _security      = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository       _auditLog      = Substitute.For<IAuditLogRepository>();
    private LoginCommandHandler _sut = null!;

    public LoginAuditTests()
    {
        _sut = new LoginCommandHandler(
            _users, _hasher, _jwt, _refreshTokens, _security, _auditLog);
    }

    private static User ActiveUser() =>
        User.Create(
            new Email("u@test.com"),
            "hashed",
            UserRole.Operator);

    [Fact]
    public async Task Handle_SuccessfulLogin_AppendsUserLoggedInAuditEntry()
    {
        var user = ActiveUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), default).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwt.Generate(Arg.Any<User>()).Returns("jwt-token");

        await _sut.Handle(new LoginCommand("u@test.com", "Pass", null), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserLoggedIn), default);
    }

    [Fact]
    public async Task Handle_FailedLogin_DoesNotAppendUserLoggedInAuditEntry()
    {
        _users.FindByEmailAsync(Arg.Any<Email>(), default).Returns((User?)null);

        try { await _sut.Handle(new LoginCommand("u@test.com", "wrong", null), default); }
        catch { /* expected */ }

        await _auditLog.DidNotReceive().AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserLoggedIn), default);
    }
}
