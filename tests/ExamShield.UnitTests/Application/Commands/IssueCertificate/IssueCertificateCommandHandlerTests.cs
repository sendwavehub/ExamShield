using ExamShield.Application.Commands.IssueCertificate;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.IssueCertificate;

public sealed class IssueCertificateCommandHandlerTests
{
    private readonly IDeviceRepository _devices = Substitute.For<IDeviceRepository>();
    private readonly IDeviceCertificateRepository _certificates = Substitute.For<IDeviceCertificateRepository>();
    private readonly IssueCertificateCommandHandler _sut;

    private const string PemKey = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYFK4EEAAoDQgAEtest==\n-----END PUBLIC KEY-----";

    public IssueCertificateCommandHandlerTests() =>
        _sut = new IssueCertificateCommandHandler(_devices, _certificates);

    private Device MakeDevice()
    {
        var device = Device.Register("Scanner-01", new PublicKey(new byte[32]));
        _devices.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);
        return device;
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsCertificateResult()
    {
        var device = MakeDevice();
        var result = await _sut.Handle(
            new IssueCertificateCommand(device.Id.Value, PemKey, 365), default);

        result.CertificateId.Should().NotBeEmpty();
        result.DeviceId.Should().Be(device.Id.Value);
        result.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsCertificate()
    {
        var device = MakeDevice();
        await _sut.Handle(new IssueCertificateCommand(device.Id.Value, PemKey, 180), default);
        await _certificates.Received(1).AddAsync(Arg.Any<DeviceCertificate>(), default);
    }

    [Fact]
    public async Task Handle_DeviceNotFound_ThrowsDeviceNotFoundException()
    {
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>())
                .Returns((Device?)null);

        var act = () => _sut.Handle(new IssueCertificateCommand(Guid.NewGuid(), PemKey, 365), default);

        await act.Should().ThrowAsync<DeviceNotFoundException>();
    }

    [Fact]
    public async Task Handle_EmptyPublicKey_ThrowsArgumentException()
    {
        var device = MakeDevice();
        var act = () => _sut.Handle(new IssueCertificateCommand(device.Id.Value, "   ", 365), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_ZeroValidDays_ThrowsArgumentOutOfRange()
    {
        var device = MakeDevice();
        var act = () => _sut.Handle(new IssueCertificateCommand(device.Id.Value, PemKey, 0), default);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_StoredPublicKeyIsTrimmed()
    {
        var device = MakeDevice();
        DeviceCertificate? saved = null;
        await _certificates.AddAsync(
            Arg.Do<DeviceCertificate>(c => saved = c), Arg.Any<CancellationToken>());

        await _sut.Handle(new IssueCertificateCommand(device.Id.Value, $"  {PemKey}  ", 365), default);

        saved.Should().NotBeNull();
        saved!.PublicKeyPem.Should().Be(PemKey);
    }
}
