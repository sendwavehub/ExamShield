using ExamShield.Application.Queries.GetDeviceCertificates;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries.GetDeviceCertificates;

public sealed class GetDeviceCertificatesQueryHandlerTests
{
    private readonly IDeviceCertificateRepository _certs = Substitute.For<IDeviceCertificateRepository>();
    private readonly GetDeviceCertificatesQueryHandler _sut;

    public GetDeviceCertificatesQueryHandlerTests() =>
        _sut = new GetDeviceCertificatesQueryHandler(_certs);

    private static DeviceCertificate MakeCert(DeviceId deviceId, int validDays = 365) =>
        DeviceCertificate.Issue(deviceId, "-----BEGIN PUBLIC KEY-----\ntest\n-----END PUBLIC KEY-----", validDays);

    [Fact]
    public async Task Handle_ReturnsCertificatesForDevice()
    {
        var deviceId = new DeviceId(Guid.NewGuid());
        var certs = new[] { MakeCert(deviceId), MakeCert(deviceId) };
        _certs.GetAllForDeviceAsync(deviceId, Arg.Any<CancellationToken>())
              .Returns(certs);

        var result = await _sut.Handle(new GetDeviceCertificatesQuery(deviceId.Value), default);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.DeviceId.Should().Be(deviceId.Value));
    }

    [Fact]
    public async Task Handle_MapsIsValidCorrectly()
    {
        var deviceId = new DeviceId(Guid.NewGuid());
        var activeCert = MakeCert(deviceId, 365);
        var revokedCert = MakeCert(deviceId, 365);
        revokedCert.Revoke("Compromised");

        _certs.GetAllForDeviceAsync(deviceId, Arg.Any<CancellationToken>())
              .Returns(new[] { activeCert, revokedCert });

        var result = await _sut.Handle(new GetDeviceCertificatesQuery(deviceId.Value), default);

        result.First(c => c.Id == activeCert.Id).IsValid.Should().BeTrue();
        result.First(c => c.Id == revokedCert.Id).IsValid.Should().BeFalse();
        result.First(c => c.Id == revokedCert.Id).RevocationReason.Should().Be("Compromised");
    }

    [Fact]
    public async Task Handle_EmptyDevice_ReturnsEmptyList()
    {
        var deviceId = new DeviceId(Guid.NewGuid());
        _certs.GetAllForDeviceAsync(deviceId, Arg.Any<CancellationToken>())
              .Returns(Array.Empty<DeviceCertificate>());

        var result = await _sut.Handle(new GetDeviceCertificatesQuery(deviceId.Value), default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields()
    {
        var deviceId = new DeviceId(Guid.NewGuid());
        var cert = MakeCert(deviceId);
        _certs.GetAllForDeviceAsync(deviceId, Arg.Any<CancellationToken>())
              .Returns(new[] { cert });

        var result = await _sut.Handle(new GetDeviceCertificatesQuery(deviceId.Value), default);

        var dto = result.Single();
        dto.Id.Should().Be(cert.Id);
        dto.PublicKeyPem.Should().Contain("PUBLIC KEY");
        dto.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        dto.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromSeconds(5));
        dto.IsRevoked.Should().BeFalse();
        dto.RevokedAt.Should().BeNull();
    }
}
