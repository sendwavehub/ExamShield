using ExamShield.Application.Commands.MfaSetup;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.MfaSetup;

public sealed class MfaSetupCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITotpService _totp = Substitute.For<ITotpService>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private MfaSetupCommandHandler CreateHandler() => new(_users, _totp, _audit);

    private static User MakeUser() =>
        User.Create(new Email("alice@exam.io"), "hash", UserRole.Invigilator);

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidOperationException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs((User?)null);

        await CreateHandler().Invoking(h =>
                h.Handle(new(Guid.NewGuid(), "alice@exam.io"), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_MfaAlreadyEnabled_ThrowsInvalidOperationException()
    {
        var user = MakeUser();
        user.SetMfaSecret("existing");
        user.EnableMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Invoking(h =>
                h.Handle(new(user.Id.Value, "alice@exam.io"), default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*already enabled*");
    }

    [Fact]
    public async Task Handle_ReturnsSecretAndQrUri()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.GenerateSecret().Returns("NEWSECRET");
        _totp.GetQrUri("NEWSECRET", "alice@exam.io").Returns("otpauth://totp/...");

        var result = await CreateHandler().Handle(new(user.Id.Value, "alice@exam.io"), default);

        result.Secret.Should().Be("NEWSECRET");
        result.QrUri.Should().StartWith("otpauth://");
    }

    [Fact]
    public async Task Handle_SetsMfaSecretOnUser()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.GenerateSecret().Returns("MYSECRET");
        _totp.GetQrUri(Arg.Any<string>(), Arg.Any<string>()).Returns("otpauth://...");

        await CreateHandler().Handle(new(user.Id.Value, "alice@exam.io"), default);

        user.MfaSecret.Should().Be("MYSECRET");
    }

    [Fact]
    public async Task Handle_AppendsMfaSecretSetAuditLog()
    {
        var user = MakeUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.GenerateSecret().Returns("SECRET");
        _totp.GetQrUri(Arg.Any<string>(), Arg.Any<string>()).Returns("otpauth://...");

        await CreateHandler().Handle(new(user.Id.Value, "alice@exam.io"), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.MfaSecretSet),
            Arg.Any<CancellationToken>());
    }
}
