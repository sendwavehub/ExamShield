using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class DeviceCertificateTests
{
    private static readonly DeviceId DeviceId = DeviceId.New();
    private const string SamplePem =
        "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQY=\n-----END PUBLIC KEY-----";

    [Fact]
    public void Issue_CreatesValidCertificate()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 365);

        cert.DeviceId.Should().Be(DeviceId);
        cert.PublicKeyPem.Should().Be(SamplePem);
        cert.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        cert.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromSeconds(5));
        cert.IsRevoked.Should().BeFalse();
        cert.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndIsRevoked()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 365);

        cert.Revoke("Key compromised");

        cert.IsRevoked.Should().BeTrue();
        cert.RevokedAt.Should().NotBeNull();
        cert.RevocationReason.Should().Be("Key compromised");
        cert.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_Throws()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 365);
        cert.Revoke("First revocation");

        var act = () => cert.Revoke("Second revocation");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already revoked*");
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 1,
            issuedAt: DateTimeOffset.UtcNow.AddDays(-2));

        cert.IsValid.Should().BeFalse();
        cert.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Issue_EmptyPem_Throws()
    {
        var act = () => DeviceCertificate.Issue(DeviceId, "", validDays: 365);

        act.Should().Throw<ArgumentException>();
    }
}
