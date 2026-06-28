using ExamShield.Application.Commands.RevokeAllSessions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.RevokeAllSessions;

public sealed class RevokeAllSessionsCommandHandlerTests
{
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly RevokeAllSessionsCommandHandler _sut;

    public RevokeAllSessionsCommandHandlerTests() =>
        _sut = new RevokeAllSessionsCommandHandler(_tokens);

    [Fact]
    public async Task Handle_DelegatesToRevokeAllForUser()
    {
        var userId = UserId.New();
        await _sut.Handle(new RevokeAllSessionsCommand(userId.Value), default);
        await _tokens.Received(1).RevokeAllForUserAsync(userId, default);
    }

    [Fact]
    public async Task Handle_PassesCorrectUserId()
    {
        var id = Guid.NewGuid();
        await _sut.Handle(new RevokeAllSessionsCommand(id), default);
        await _tokens.Received(1).RevokeAllForUserAsync(new UserId(id), default);
    }
}
