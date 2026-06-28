using ExamShield.Application.Commands.MfaVerify;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.MfaVerify;

public sealed class MfaVerifyCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITotpService _totp = Substitute.For<ITotpService>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private MfaVerifyCommandHandler CreateHandler() => new(_users, _totp, _audit);

    private static User MakeUserWithSecret()
    {
        var u = User.Create(new Email("u@exam.io"), "hash", UserRole.Invigilator);
        u.SetMfaSecret("TOTP-SECRET");
        return u;
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidOperationException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs((User?)null);

        await CreateHandler().Invoking(h => h.Handle(new(Guid.NewGuid(), "123456"), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_NoMfaSecret_ThrowsInvalidOperationException()
    {
        var user = User.Create(new Email("u@exam.io"), "hash", UserRole.Invigilator);
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);

        await CreateHandler().Invoking(h => h.Handle(new(user.Id.Value, "123456"), default))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*setup*");
    }

    [Fact]
    public async Task Handle_InvalidCode_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUserWithSecret();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.Verify("TOTP-SECRET", "000000").Returns(false);

        await CreateHandler().Invoking(h => h.Handle(new(user.Id.Value, "000000"), default))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ValidCode_EnablesMfa()
    {
        var user = MakeUserWithSecret();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.Verify("TOTP-SECRET", "123456").Returns(true);

        await CreateHandler().Handle(new(user.Id.Value, "123456"), default);

        user.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCode_ReturnsMfaEnabledTrue()
    {
        var user = MakeUserWithSecret();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.Verify("TOTP-SECRET", "123456").Returns(true);

        var result = await CreateHandler().Handle(new(user.Id.Value, "123456"), default);

        result.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCode_AppendsMfaEnabledAuditLog()
    {
        var user = MakeUserWithSecret();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).ReturnsForAnyArgs(user);
        _totp.Verify("TOTP-SECRET", "123456").Returns(true);

        await CreateHandler().Handle(new(user.Id.Value, "123456"), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.MfaEnabled),
            Arg.Any<CancellationToken>());
    }
}
