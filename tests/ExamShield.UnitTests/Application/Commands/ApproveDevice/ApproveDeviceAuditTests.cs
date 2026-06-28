using ExamShield.Application.Commands.ApproveDevice;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.ApproveDevice;

public sealed class ApproveDeviceAuditTests
{
    private readonly IDeviceRepository   _devices  = Substitute.For<IDeviceRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly ApproveDeviceCommandHandler _sut;

    public ApproveDeviceAuditTests() =>
        _sut = new ApproveDeviceCommandHandler(_devices, _auditLog);

    [Fact]
    public async Task Handle_Approval_AppendsDeviceApprovedAuditEntry()
    {
        var device = Device.Register("Scanner-1", new PublicKey(new byte[32]));
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).Returns(device);

        await _sut.Handle(new ApproveDeviceCommand(Guid.NewGuid()), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.DeviceApproved), default);
    }
}
