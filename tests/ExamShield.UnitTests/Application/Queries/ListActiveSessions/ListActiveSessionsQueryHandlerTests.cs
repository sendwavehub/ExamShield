using ExamShield.Application.Queries.ListActiveSessions;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries.ListActiveSessions;

public sealed class ListActiveSessionsQueryHandlerTests
{
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ListActiveSessionsQueryHandler _sut;

    public ListActiveSessionsQueryHandlerTests() =>
        _sut = new ListActiveSessionsQueryHandler(_tokens);

    private static UserId SomeUser() => UserId.New();

    [Fact]
    public async Task Handle_ReturnsActiveTokensForUser()
    {
        var userId = SomeUser();
        var active = RefreshToken.Create(userId, "hash1", 7);
        _tokens.ListActiveByUserAsync(userId, Arg.Any<CancellationToken>())
               .Returns([active]);

        var result = await _sut.Handle(new ListActiveSessionsQuery(userId.Value), default);

        result.Sessions.Should().HaveCount(1);
        result.Sessions[0].Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSessions_ReturnsEmptyList()
    {
        var userId = SomeUser();
        _tokens.ListActiveByUserAsync(userId, Arg.Any<CancellationToken>())
               .Returns([]);

        var result = await _sut.Handle(new ListActiveSessionsQuery(userId.Value), default);

        result.Sessions.Should().BeEmpty();
    }
}
