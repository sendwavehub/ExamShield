using ExamShield.Application.Commands.DeactivateUser;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private DeactivateUserCommandHandler CreateHandler() =>
        new(_users, _tokens, _audit);

    private static User MakeUser() =>
        User.Create(new Email("u@exam.io"), "hash", UserRole.Invigilator);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUserNotFoundException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs((User?)null);

        await CreateHandler().Invoking(h => h.Handle(new(Guid.NewGuid()), default))
            .Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeactivatesUser()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PersistsUser()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        await _users.Received(1).SaveAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevokesAllTokensForUser()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        await _tokens.Received(1).RevokeAllForUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsUserDeactivatedAuditLog()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserDeactivated),
            Arg.Any<CancellationToken>());
    }
}
