using ExamShield.Application.Commands.BlacklistDevice;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.BlacklistDevice;

public sealed class BlacklistDeviceAuditTests
{
    private readonly IDeviceRepository   _devices  = Substitute.For<IDeviceRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly BlacklistDeviceCommandHandler _sut;

    public BlacklistDeviceAuditTests() =>
        _sut = new BlacklistDeviceCommandHandler(_devices, _auditLog);

    [Fact]
    public async Task Handle_Blacklist_AppendsDeviceBlacklistedAuditEntry()
    {
        var device = Device.Register("Scanner-1", new PublicKey(new byte[32]));
        device.Approve();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).Returns(device);

        await _sut.Handle(
            new BlacklistDeviceCommand(Guid.NewGuid(), "Suspected tamper"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.DeviceBlacklisted), default);
    }
}
