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

    [Theory]
    [InlineData("   ")]
    public void Issue_WhitespacePem_Throws(string pem)
    {
        var act = () => DeviceCertificate.Issue(DeviceId, pem, validDays: 365);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_NullDeviceId_ThrowsArgumentNullException()
    {
        var act = () => DeviceCertificate.Issue(null!, SamplePem, validDays: 365);

        act.Should().Throw<ArgumentNullException>().WithParameterName("deviceId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_ZeroOrNegativeValidDays_ThrowsArgumentOutOfRangeException(int days)
    {
        var act = () => DeviceCertificate.Issue(DeviceId, SamplePem, validDays: days);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("validDays");
    }

    [Fact]
    public void Issue_AssignsNonEmptyId()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 30);

        cert.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Issue_TwoCertificates_HaveDifferentIds()
    {
        var a = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 30);
        var b = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 30);

        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Issue_TrimsPublicKeyPem()
    {
        var cert = DeviceCertificate.Issue(DeviceId, "  pem-data  ", validDays: 30);

        cert.PublicKeyPem.Should().Be("pem-data");
    }

    [Fact]
    public void Issue_UsesProvidedIssuedAt()
    {
        var fixedTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 30, issuedAt: fixedTime);

        cert.IssuedAt.Should().Be(fixedTime);
        cert.ExpiresAt.Should().Be(fixedTime.AddDays(30));
    }

    [Fact]
    public void Revoke_TrimsReason()
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 365);

        cert.Revoke("  trimmed reason  ");

        cert.RevocationReason.Should().Be("trimmed reason");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Revoke_EmptyReason_ThrowsArgumentException(string reason)
    {
        var cert = DeviceCertificate.Issue(DeviceId, SamplePem, validDays: 365);

        var act = () => cert.Revoke(reason);

        act.Should().Throw<ArgumentException>();
    }
}
