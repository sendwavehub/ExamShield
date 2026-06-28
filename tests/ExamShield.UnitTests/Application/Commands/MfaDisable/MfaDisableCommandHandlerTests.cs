using ExamShield.Application.Commands.MfaDisable;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.MfaDisable;

public sealed class MfaDisableCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private MfaDisableCommandHandler CreateHandler() => new(_users, _audit);

    private static User MakeUserWithMfa()
    {
        var u = User.Create(new Email("u@exam.io"), "hash", UserRole.Invigilator);
        u.SetMfaSecret("TOTP-SECRET");
        u.EnableMfa();
        return u;
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidOperationException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs((User?)null);

        await CreateHandler().Invoking(h => h.Handle(new(Guid.NewGuid()), default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_DisablesMfa()
    {
        var user = MakeUserWithMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        user.MfaEnabled.Should().BeFalse();
        user.MfaSecret.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsResultWithMfaEnabledFalse()
    {
        var user = MakeUserWithMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        var result = await CreateHandler().Handle(new(user.Id.Value), default);

        result.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PersistsUser()
    {
        var user = MakeUserWithMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        await _users.Received(1).SaveAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsMfaDisabledAuditLog()
    {
        var user = MakeUserWithMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Handle(new(user.Id.Value), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.MfaDisabled),
            Arg.Any<CancellationToken>());
    }
}
