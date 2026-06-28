using ExamShield.Application.Commands.Refresh;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class RefreshTokenAuditTests
{
    private readonly IRefreshTokenRepository  _tokens   = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository          _users    = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService         _jwt      = Substitute.For<IJwtTokenService>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository      _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenAuditTests() =>
        _sut = new RefreshTokenCommandHandler(_tokens, _users, _jwt, _security, _auditLog);

    private static ExamShield.Domain.Entities.RefreshToken ActiveToken(UserId userId)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("raw-token")));
        return ExamShield.Domain.Entities.RefreshToken.Create(userId, hash, expiryDays: 7);
    }

    [Fact]
    public async Task Handle_SuccessfulRefresh_AppendsTokenRefreshedAuditEntry()
    {
        var user  = User.Create(new Email("user@test.com"), "hash", UserRole.Operator);
        var token = ActiveToken(user.Id);
        _tokens.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _jwt.Generate(Arg.Any<User>()).Returns("new-jwt");

        await _sut.Handle(new RefreshTokenCommand("raw-token"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.TokenRefreshed),
            default);
    }
}
