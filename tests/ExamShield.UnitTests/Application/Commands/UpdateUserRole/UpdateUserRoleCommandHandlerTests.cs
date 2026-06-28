using ExamShield.Application.Commands.UpdateUserRole;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private UpdateUserRoleCommandHandler CreateHandler() => new(_users, _tokens, _audit);

    private static User MakeUser() =>
        User.Create(new Email("u@exam.io"), "hash", UserRole.Invigilator);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUserNotFoundException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs((User?)null);

        await CreateHandler().Invoking(h =>
                h.Handle(new(Guid.NewGuid(), "Administrator"), default))
            .Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidRoleString_ThrowsArgumentException()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Invoking(h =>
                h.Handle(new(user.Id.Value, "SuperUltraAdmin"), default))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task Handle_ChangesUserRole()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value, "Administrator"), default);

        user.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task Handle_RoleParsingIsCaseInsensitive()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value, "administrator"), default);

        user.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task Handle_RevokesAllTokensOnRoleChange()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value, "Auditor"), default);

        await _tokens.Received(1).RevokeAllForUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsUserRoleChangedAuditLog()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value, "Supervisor"), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.UserRoleChanged),
            Arg.Any<CancellationToken>());
    }
}
