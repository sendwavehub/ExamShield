using ExamShield.Application.Commands.Login;
using ExamShield.Application.Commands.Refresh;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using DomainRefreshToken = ExamShield.Domain.Entities.RefreshToken;
using DomainUser = ExamShield.Domain.Entities.User;
using DomainSecurityEvent = ExamShield.Domain.Entities.SecurityEvent;

namespace ExamShield.UnitTests.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly ISecurityEventRepository _securityEvents = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests() =>
        _sut = new(_refreshTokens, _users, _jwt, _securityEvents, _auditLog);

    private static string HashRaw(string raw) =>
        Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw)));

    private static DomainUser MakeUser() =>
        DomainUser.Create(new Email("t@ex.io"), "hash", UserRole.Invigilator);

    private static DomainRefreshToken ActiveToken(UserId userId) =>
        DomainRefreshToken.Create(userId, HashRaw("raw"), expiryDays: 7);

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsInvalidCredentials()
    {
        _refreshTokens.FindByHashAsync(Arg.Any<string>(), default)
            .Returns((DomainRefreshToken?)null);

        await FluentActions.Invoking(() => _sut.Handle(new("bad"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidCredentials()
    {
        var user = MakeUser();
        var expired = DomainRefreshToken.Create(user.Id, HashRaw("exp"), expiryDays: 0);
        _refreshTokens.FindByHashAsync(HashRaw("exp"), default).Returns(expired);

        await FluentActions.Invoking(() => _sut.Handle(new("exp"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_RevokedNonExpiredToken_LogsTokenTheftDetected()
    {
        var user = MakeUser();
        var token = ActiveToken(user.Id);
        token.Revoke(); // revoked but not expired
        _refreshTokens.FindByHashAsync(HashRaw("raw"), default).Returns(token);

        await FluentActions.Invoking(() => _sut.Handle(new("raw"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();

        await _securityEvents.Received(1).AddAsync(
            Arg.Is<DomainSecurityEvent>(e => e.EventType == SecurityEventType.TokenTheftDetected),
            default);
    }

    [Fact]
    public async Task Handle_RevokedNonExpiredToken_RevokesAllUserTokens()
    {
        var user = MakeUser();
        var token = ActiveToken(user.Id);
        token.Revoke();
        _refreshTokens.FindByHashAsync(HashRaw("raw"), default).Returns(token);

        await FluentActions.Invoking(() => _sut.Handle(new("raw"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();

        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, default);
    }

    [Fact]
    public async Task Handle_ValidToken_AddNewRefreshToken()
    {
        var user = MakeUser();
        var token = ActiveToken(user.Id);
        _refreshTokens.FindByHashAsync(HashRaw("raw"), default).Returns(token);
        _users.GetByIdAsync(user.Id, default).Returns(user);
        _jwt.Generate(user).Returns("jwt");

        var result = await _sut.Handle(new("raw"), default);

        await _refreshTokens.Received(1).AddAsync(Arg.Any<DomainRefreshToken>(), default);
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }
}
