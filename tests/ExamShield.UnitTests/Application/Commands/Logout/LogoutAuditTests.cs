using ExamShield.Application.Commands.Logout;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using DomainRefreshToken = ExamShield.Domain.Entities.RefreshToken;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Logout;

public sealed class LogoutAuditTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogRepository     _auditLog      = Substitute.For<IAuditLogRepository>();
    private readonly LogoutCommandHandler    _sut;

    public LogoutAuditTests() =>
        _sut = new LogoutCommandHandler(_refreshTokens, _auditLog);

    [Fact]
    public async Task Handle_ActiveRefreshToken_AppendsUserLoggedOutAuditEntry()
    {
        var token = DomainRefreshToken.Create(UserId.New(), "hash", expiryDays: 7);
        _refreshTokens.FindByHashAsync(Arg.Any<string>(), default).Returns(token);

        await _sut.Handle(new LogoutCommand("raw-token"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserLoggedOut), default);
    }

    [Fact]
    public async Task Handle_NoMatchingToken_DoesNotAppendAuditEntry()
    {
        _refreshTokens.FindByHashAsync(Arg.Any<string>(), default).Returns((DomainRefreshToken?)null);

        await _sut.Handle(new LogoutCommand("raw-token"), default);

        await _auditLog.DidNotReceive().AppendAsync(Arg.Any<AuditLog>(), default);
    }
}
