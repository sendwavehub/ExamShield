using ExamShield.Application.Commands.RegisterDevice;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class RegisterDeviceCommandHandlerTests
{
    private readonly IDeviceRepository _repository = Substitute.For<IDeviceRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RegisterDeviceCommandHandler _sut;

    private static readonly byte[] ValidKeyBytes = new byte[] { 0x04, 0x01, 0x02 };

    public RegisterDeviceCommandHandlerTests() =>
        _sut = new RegisterDeviceCommandHandler(_repository, _auditLog);

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsNonEmptyDeviceId()
    {
        var result = await _sut.Handle(
            new RegisterDeviceCommand("Scanner-01", ValidKeyBytes), CancellationToken.None);

        result.DeviceId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidRequest_SavesDevice()
    {
        await _sut.Handle(
            new RegisterDeviceCommand("Scanner-01", ValidKeyBytes), CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Device>(d => d.Name == "Scanner-01"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyPublicKeyBytes_ThrowsArgumentException()
    {
        var act = () => _sut.Handle(
            new RegisterDeviceCommand("Scanner-01", Array.Empty<byte>()), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => _sut.Handle(
            new RegisterDeviceCommand("", ValidKeyBytes), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
