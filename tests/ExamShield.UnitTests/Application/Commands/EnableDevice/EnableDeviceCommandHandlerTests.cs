using ExamShield.Application.Commands.EnableDevice;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.EnableDevice;

public sealed class EnableDeviceCommandHandlerTests
{
    private readonly IDeviceRepository _devices = Substitute.For<IDeviceRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private EnableDeviceCommandHandler CreateHandler() => new(_devices, _audit);

    private static Device MakeDisabledDevice()
    {
        var d = Device.Register("Scanner B", new PublicKey(new byte[32]));
        d.Disable();
        return d;
    }

    [Fact]
    public async Task Handle_DeviceNotFound_ThrowsDeviceNotFoundException()
    {
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs((Device?)null);

        await CreateHandler().Invoking(h => h.Handle(new(Guid.NewGuid()), default))
            .Should().ThrowAsync<DeviceNotFoundException>();
    }

    [Fact]
    public async Task Handle_EnablesDevice_StatusBecomesApproved()
    {
        var device = MakeDisabledDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        device.Status.Should().Be(DeviceStatus.Approved);
    }

    [Fact]
    public async Task Handle_PersistsDevice()
    {
        var device = MakeDisabledDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        await _devices.Received(1).SaveAsync(device, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsDeviceEnabledAuditLog()
    {
        var device = MakeDisabledDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.DeviceEnabled),
            Arg.Any<CancellationToken>());
    }
}
