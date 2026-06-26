using System.Security.Cryptography;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Security;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Infrastructure.Security;

public sealed class EcdsaSignatureVerificationServiceTests
{
    private readonly EcdsaSignatureVerificationService _sut = new();

    [Fact]
    public void Verify_WithValidSignature_ReturnsTrue()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = new PublicKey(ecdsa.ExportSubjectPublicKeyInfo());
        var hash = Hash.FromHex(new string('a', 64));
        var signature = new Signature(ecdsa.SignHash(hash.ToBytes()));

        _sut.Verify(hash, signature, publicKey).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithTamperedHash_ReturnsFalse()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = new PublicKey(ecdsa.ExportSubjectPublicKeyInfo());
        var originalHash = Hash.FromHex(new string('a', 64));
        var signature = new Signature(ecdsa.SignHash(originalHash.ToBytes()));

        var differentHash = Hash.FromHex(new string('b', 64));

        _sut.Verify(differentHash, signature, publicKey).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithWrongKey_ReturnsFalse()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var publicKey = new PublicKey(otherKey.ExportSubjectPublicKeyInfo()); // mismatch
        var hash = Hash.FromHex(new string('a', 64));
        var signature = new Signature(signingKey.SignHash(hash.ToBytes()));

        _sut.Verify(hash, signature, publicKey).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCorruptSignatureBytes_ReturnsFalse()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = new PublicKey(ecdsa.ExportSubjectPublicKeyInfo());
        var hash = Hash.FromHex(new string('a', 64));
        var badSignature = new Signature(new byte[64]); // all zeros — invalid ECDSA signature

        _sut.Verify(hash, badSignature, publicKey).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithInvalidPublicKeyBytes_ReturnsFalse()
    {
        var badKey = new PublicKey(new byte[] { 0xFF, 0xFF, 0xFF }); // not a valid DER key
        var hash = Hash.FromHex(new string('a', 64));
        var signature = new Signature(new byte[64]);

        _sut.Verify(hash, signature, badKey).Should().BeFalse();
    }
}
