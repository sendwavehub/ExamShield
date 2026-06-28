using System.Security.Cryptography;
using System.Text;
using ExamShield.Application.Commands.Logout;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Logout;

using DomainRefreshToken = ExamShield.Domain.Entities.RefreshToken;

public sealed class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private LogoutCommandHandler CreateHandler() => new(_tokens, _audit);

    private static string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_TokenNotFound_DoesNotRevoke_NoAuditLog()
    {
        _tokens.FindByHashAsync(Arg.Any<string>(), default).ReturnsForAnyArgs((DomainRefreshToken?)null);

        await CreateHandler().Handle(new("unknown-token"), default);

        await _tokens.DidNotReceive().SaveAsync(Arg.Any<DomainRefreshToken>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive().AppendAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesIt()
    {
        var raw = "my-refresh-token";
        var hash = HashToken(raw);
        var token = DomainRefreshToken.Create(UserId.New(), hash, 7);
        _tokens.FindByHashAsync(Arg.Any<string>(), default).ReturnsForAnyArgs(token);

        await CreateHandler().Handle(new(raw), default);

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActiveToken_PersistsRevocation()
    {
        var raw = "my-refresh-token";
        var token = DomainRefreshToken.Create(UserId.New(), HashToken(raw), 7);
        _tokens.FindByHashAsync(Arg.Any<string>(), default).ReturnsForAnyArgs(token);

        await CreateHandler().Handle(new(raw), default);

        await _tokens.Received(1).SaveAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveToken_AppendsUserLoggedOutAuditLog()
    {
        var raw = "my-refresh-token";
        var token = DomainRefreshToken.Create(UserId.New(), HashToken(raw), 7);
        _tokens.FindByHashAsync(Arg.Any<string>(), default).ReturnsForAnyArgs(token);

        await CreateHandler().Handle(new(raw), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserLoggedOut),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_SkipsRevokeAndAudit()
    {
        var raw = "already-revoked";
        var token = DomainRefreshToken.Create(UserId.New(), HashToken(raw), 7);
        token.Revoke();
        _tokens.FindByHashAsync(Arg.Any<string>(), default).ReturnsForAnyArgs(token);

        await CreateHandler().Handle(new(raw), default);

        await _tokens.DidNotReceive().SaveAsync(Arg.Any<DomainRefreshToken>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive().AppendAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
    }
}
