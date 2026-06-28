using ExamShield.Application.Commands.RevokeCertificate;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.RevokeCertificate;

public sealed class RevokeCertificateCommandHandlerTests
{
    private readonly IDeviceCertificateRepository _certs = Substitute.For<IDeviceCertificateRepository>();
    private readonly RevokeCertificateCommandHandler _sut;

    public RevokeCertificateCommandHandlerTests() =>
        _sut = new RevokeCertificateCommandHandler(_certs);

    private DeviceCertificate MakeCert()
    {
        var cert = DeviceCertificate.Issue(new DeviceId(Guid.NewGuid()), "-----BEGIN PUBLIC KEY-----\ntest\n-----END PUBLIC KEY-----", 365);
        _certs.GetByIdAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);
        return cert;
    }

    [Fact]
    public async Task Handle_ValidCert_SetsRevokedAt()
    {
        var cert = MakeCert();
        await _sut.Handle(new RevokeCertificateCommand(cert.Id, "Compromised key"), default);
        cert.IsRevoked.Should().BeTrue();
        cert.RevocationReason.Should().Be("Compromised key");
    }

    [Fact]
    public async Task Handle_ValidCert_PersistsUpdate()
    {
        var cert = MakeCert();
        await _sut.Handle(new RevokeCertificateCommand(cert.Id, "Key rotation"), default);
        await _certs.Received(1).UpdateAsync(cert, default);
    }

    [Fact]
    public async Task Handle_CertNotFound_ThrowsKeyNotFoundException()
    {
        _certs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
              .Returns((DeviceCertificate?)null);

        var act = () => _sut.Handle(new RevokeCertificateCommand(Guid.NewGuid(), "reason"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyRevoked_ThrowsInvalidOperationException()
    {
        var cert = MakeCert();
        cert.Revoke("First revocation");

        var act = () => _sut.Handle(new RevokeCertificateCommand(cert.Id, "Second revocation"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already revoked*");
    }

    [Fact]
    public async Task Handle_EmptyReason_ThrowsArgumentException()
    {
        var cert = MakeCert();
        var act = () => _sut.Handle(new RevokeCertificateCommand(cert.Id, "  "), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
