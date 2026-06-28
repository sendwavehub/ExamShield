using ExamShield.Application.Commands.RevokeSession;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using DomainRefreshToken = ExamShield.Domain.Entities.RefreshToken;

namespace ExamShield.UnitTests.Application.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandlerTests
{
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly RevokeSessionCommandHandler _sut;

    public RevokeSessionCommandHandlerTests() =>
        _sut = new RevokeSessionCommandHandler(_tokens);

    [Fact]
    public async Task Handle_WithOwnToken_RevokesIt()
    {
        var userId = UserId.New();
        var token = DomainRefreshToken.Create(userId, "hash", 7);
        _tokens.FindByIdAsync(token.Id, Arg.Any<CancellationToken>()).Returns(token);

        await _sut.Handle(new RevokeSessionCommand(userId.Value, token.Id), default);

        token.IsRevoked.Should().BeTrue();
        await _tokens.Received(1).SaveAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnotherUsersToken_ThrowsUnauthorized()
    {
        var owner = UserId.New();
        var requester = UserId.New();
        var token = DomainRefreshToken.Create(owner, "hash", 7);
        _tokens.FindByIdAsync(token.Id, Arg.Any<CancellationToken>()).Returns(token);

        var act = () => _sut.Handle(new RevokeSessionCommand(requester.Value, token.Id), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsKeyNotFound()
    {
        _tokens.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns((DomainRefreshToken?)null);

        var act = () => _sut.Handle(new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
