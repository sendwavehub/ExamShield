using ExamShield.Application.Commands.MfaVerify;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands.MfaVerify;

public sealed class MfaVerifyAuditTests
{
    private readonly IUserRepository     _users    = Substitute.For<IUserRepository>();
    private readonly ITotpService        _totp     = Substitute.For<ITotpService>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly MfaVerifyCommandHandler _sut;

    public MfaVerifyAuditTests() => _sut = new MfaVerifyCommandHandler(_users, _totp, _auditLog);

    [Fact]
    public async Task Handle_SuccessfulVerification_AppendsMfaEnabledAuditEntry()
    {
        var user = User.Create(new Email("mfa@test.com"), "hash", UserRole.Operator);
        user.SetMfaSecret("SECRET");
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        _totp.Verify("SECRET", "123456").Returns(true);

        await _sut.Handle(new MfaVerifyCommand(user.Id.Value, "123456"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.MfaEnabled),
            default);
    }
}
