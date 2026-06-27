using ExamShield.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Security;

public sealed class EcdsaServerSigningServiceTests
{
    private readonly EcdsaServerSigningService _sut = new(privateKeyPem: null);

    [Fact]
    public void Sign_ThenVerify_ReturnsTrue()
    {
        var data = "capture|registered|123456";
        var signature = _sut.Sign(data);
        _sut.Verify(data, signature).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithTamperedData_ReturnsFalse()
    {
        var data = "capture|registered|123456";
        var signature = _sut.Sign(data);
        _sut.Verify("capture|registered|999999", signature).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCorruptSignature_ReturnsFalse()
    {
        var data = "any payload";
        _sut.Verify(data, "not-valid-base64!!!").Should().BeFalse();
    }

    [Fact]
    public void PublicKeyBase64_IsNonEmpty()
    {
        _sut.PublicKeyBase64.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TwoInstances_WithSameKey_CanCrossVerify()
    {
        var pem = _sut.ExportPrivateKeyPem();
        var other = new EcdsaServerSigningService(pem);

        var data = "cross-verify test";
        var sig = _sut.Sign(data);
        other.Verify(data, sig).Should().BeTrue();
    }
}
