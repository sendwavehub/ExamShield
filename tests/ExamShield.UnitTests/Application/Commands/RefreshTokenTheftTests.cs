using ExamShield.Application.Commands.Refresh;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class RefreshTokenTheftTests
{
    private readonly IRefreshTokenRepository    _tokens   = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository            _users    = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService           _jwt      = Substitute.For<IJwtTokenService>();
    private readonly ISecurityEventRepository   _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository        _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenTheftTests() =>
        _sut = new RefreshTokenCommandHandler(_tokens, _users, _jwt, _security, _auditLog);

    private static ExamShield.Domain.Entities.RefreshToken RevokedNotExpiredToken(UserId userId)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("stolen-token")));
        var token = ExamShield.Domain.Entities.RefreshToken.Create(userId, hash, expiryDays: 7);
        token.Revoke();
        return token;
    }

    [Fact]
    public async Task Handle_RevokedButNotExpiredToken_ThrowsInvalidCredentialsException()
    {
        var userId = UserId.New();
        var token  = RevokedNotExpiredToken(userId);

        _tokens.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var act = () => _sut.Handle(new RefreshTokenCommand("stolen-token"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_RevokedButNotExpiredToken_RevokesAllTokensForUser()
    {
        var userId = UserId.New();
        var token  = RevokedNotExpiredToken(userId);

        _tokens.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        try { await _sut.Handle(new RefreshTokenCommand("stolen-token"), default); } catch { }

        await _tokens.Received(1).RevokeAllForUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevokedButNotExpiredToken_LogsTokenTheftSecurityEvent()
    {
        var userId = UserId.New();
        var token  = RevokedNotExpiredToken(userId);

        _tokens.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        try { await _sut.Handle(new RefreshTokenCommand("stolen-token"), default); } catch { }

        await _security.Received(1).AddAsync(
            Arg.Is<SecurityEvent>(e => e.EventType == SecurityEventType.TokenTheftDetected
                                    && e.Severity  == SecuritySeverity.Critical),
            Arg.Any<CancellationToken>());
    }
}
