using ExamShield.Application.Commands.MfaSetup;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands.MfaSetup;

public sealed class MfaSetupAuditTests
{
    private readonly IUserRepository     _users    = Substitute.For<IUserRepository>();
    private readonly ITotpService        _totp     = Substitute.For<ITotpService>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly MfaSetupCommandHandler _sut;

    public MfaSetupAuditTests() => _sut = new MfaSetupCommandHandler(_users, _totp, _auditLog);

    [Fact]
    public async Task Handle_ValidSetup_AppendsMfaSecretSetAuditEntry()
    {
        var user = User.Create(new Email("mfa@test.com"), "hash", UserRole.Operator);
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        _totp.GenerateSecret().Returns("TOTP_SECRET");
        _totp.GetQrUri(Arg.Any<string>(), Arg.Any<string>()).Returns("otpauth://...");

        await _sut.Handle(new MfaSetupCommand(user.Id.Value, "mfa@test.com"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.MfaSecretSet),
            default);
    }
}
