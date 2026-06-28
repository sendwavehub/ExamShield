using ExamShield.Application.Commands.DisableDevice;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.DisableDevice;

public sealed class DisableDeviceCommandHandlerTests
{
    private readonly IDeviceRepository _devices = Substitute.For<IDeviceRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private DisableDeviceCommandHandler CreateHandler() => new(_devices, _audit);

    private static Device MakeApprovedDevice()
    {
        var d = Device.Register("Scanner A", new PublicKey(new byte[32]));
        d.Approve();
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
    public async Task Handle_DisablesDevice()
    {
        var device = MakeApprovedDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        device.Status.Should().Be(DeviceStatus.Disabled);
    }

    [Fact]
    public async Task Handle_PersistsDevice()
    {
        var device = MakeApprovedDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        await _devices.Received(1).SaveAsync(device, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsDeviceDisabledAuditLog()
    {
        var device = MakeApprovedDevice();
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), default).ReturnsForAnyArgs(device);

        await CreateHandler().Handle(new(device.Id.Value), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.DeviceDisabled),
            Arg.Any<CancellationToken>());
    }
}
