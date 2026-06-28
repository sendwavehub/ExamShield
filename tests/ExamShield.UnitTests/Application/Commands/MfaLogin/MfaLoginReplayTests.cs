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

public sealed class MfaLoginReplayTests
{
    private readonly IUserRepository          _users         = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher          _hasher        = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService         _jwt           = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository  _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITotpService             _totp          = Substitute.For<ITotpService>();
    private readonly ITotpUsedCodeCache       _usedCodes     = Substitute.For<ITotpUsedCodeCache>();
    private readonly IAuditLogRepository      _auditLog      = Substitute.For<IAuditLogRepository>();
    private readonly MfaLoginCommandHandler   _sut;

    private const string Email    = "mfa@test.com";
    private const string Password = "Passw0rd!";
    private const string Code     = "123456";

    public MfaLoginReplayTests()
    {
        _sut = new MfaLoginCommandHandler(
            _users, _hasher, _jwt, _refreshTokens, _totp, _usedCodes, _auditLog);

        var user = User.Create(new Email(Email), "hashed", UserRole.Operator);
        user.SetMfaSecret("JBSWY3DPEHPK3PXP");
        user.EnableMfa();
        _users.FindByEmailAsync(Arg.Any<Email>(), default).Returns(user);
        _hasher.Verify(Password, "hashed").Returns(true);
        _totp.Verify(Arg.Any<string>(), Code).Returns(true);
        _jwt.GenerateWithMfa(Arg.Any<User>()).Returns("jwt-token");
    }

    [Fact]
    public async Task Handle_ReusedTotpCode_ThrowsUnauthorized()
    {
        _usedCodes.IsUsedAsync(Arg.Any<string>(), Code, default).Returns(true);

        var act = () => _sut.Handle(
            new MfaLoginCommand(Email, Password, Code), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*replay*");
    }

    [Fact]
    public async Task Handle_FreshTotpCode_MarksCodeAsUsed()
    {
        _usedCodes.IsUsedAsync(Arg.Any<string>(), Code, default).Returns(false);

        await _sut.Handle(new MfaLoginCommand(Email, Password, Code), default);

        await _usedCodes.Received(1).MarkUsedAsync(Arg.Any<string>(), Code, default);
    }

    [Fact]
    public async Task Handle_FreshTotpCode_Succeeds()
    {
        _usedCodes.IsUsedAsync(Arg.Any<string>(), Code, default).Returns(false);

        var result = await _sut.Handle(
            new MfaLoginCommand(Email, Password, Code), default);

        result.Token.Should().Be("jwt-token");
    }
}
