using ExamShield.Application.Commands.Login;
using ExamShield.Application.Commands.MfaLogin;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.MfaLogin;

public sealed class MfaLoginAuditTests
{
    private readonly IUserRepository          _users         = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher          _hasher        = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService         _jwt           = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository  _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITotpService             _totp          = Substitute.For<ITotpService>();
    private readonly ITotpUsedCodeCache       _usedCodes     = Substitute.For<ITotpUsedCodeCache>();
    private readonly IAuditLogRepository      _auditLog      = Substitute.For<IAuditLogRepository>();
    private readonly MfaLoginCommandHandler   _sut;

    public MfaLoginAuditTests() =>
        _sut = new MfaLoginCommandHandler(
            _users, _hasher, _jwt, _refreshTokens, _totp, _usedCodes, _auditLog);

    private static User MfaUser()
    {
        var user = User.Create(new Email("mfa@test.com"), "hashed", UserRole.Operator);
        user.SetMfaSecret("SECRET");
        user.EnableMfa();
        return user;
    }

    [Fact]
    public async Task Handle_SuccessfulMfaLogin_AppendsUserLoggedInAuditEntry()
    {
        var user = MfaUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), default).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _totp.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _usedCodes.IsUsedAsync(Arg.Any<string>(), Arg.Any<string>(), default).Returns(false);
        _jwt.Generate(Arg.Any<User>()).Returns("jwt-token");

        await _sut.Handle(new MfaLoginCommand("mfa@test.com", "Pass", "123456"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserLoggedIn), default);
    }

    [Fact]
    public async Task Handle_InvalidPassword_DoesNotAppendAuditEntry()
    {
        _users.FindByEmailAsync(Arg.Any<Email>(), default).Returns((User?)null);

        try { await _sut.Handle(new MfaLoginCommand("x@x.com", "bad", "000000"), default); }
        catch { /* expected */ }

        await _auditLog.DidNotReceive().AppendAsync(Arg.Any<AuditLog>(), default);
    }
}
