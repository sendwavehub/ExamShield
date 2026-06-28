using System.Security.Cryptography;
using ExamShield.Domain.Interfaces;
using ExamShield.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ExamShield.UnitTests.Infrastructure.Security;

public sealed class AesGcmImageEncryptionServiceTests
{
    private static readonly string ValidKey = Convert.ToBase64String(new byte[32]);

    private static IImageEncryptionService BuildSut(string keyBase64 = "") =>
        new AesGcmImageEncryptionService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKeyBase64"] = string.IsNullOrEmpty(keyBase64) ? ValidKey : keyBase64
            })
            .Build());

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalBytes()
    {
        var sut = BuildSut();
        var original = "secret exam image"u8.ToArray();

        var (ciphertext, encryptedDek) = sut.Encrypt(original);
        var decrypted = sut.Decrypt(ciphertext, encryptedDek);

        decrypted.Should().Equal(original);
    }

    [Fact]
    public void Encrypt_EmptyBytes_RoundTripsCorrectly()
    {
        var sut = BuildSut();
        var (ct, dek) = sut.Encrypt([]);
        sut.Decrypt(ct, dek).Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_LargePayload_RoundTripsCorrectly()
    {
        var sut = BuildSut();
        var original = new byte[1024 * 1024]; // 1 MB
        Random.Shared.NextBytes(original);

        var (ct, dek) = sut.Encrypt(original);
        sut.Decrypt(ct, dek).Should().Equal(original);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesDifferentCiphertextEachTime()
    {
        var sut = BuildSut();
        var original = "same image"u8.ToArray();

        var (ct1, _) = sut.Encrypt(original);
        var (ct2, _) = sut.Encrypt(original);

        ct1.Should().NotEqual(ct2);
    }

    [Fact]
    public void Encrypt_CiphertextContainsNonceAndTagOverhead()
    {
        var sut = BuildSut();
        var original = new byte[100];

        var (ct, _) = sut.Encrypt(original);

        // 12-byte nonce + 100-byte ciphertext + 16-byte GCM tag = 128 bytes
        ct.Length.Should().Be(original.Length + 12 + 16);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertextTag_ThrowsCryptographicException()
    {
        var sut = BuildSut();
        var (ct, dek) = sut.Encrypt("secret"u8.ToArray());
        ct[^1] ^= 0xFF; // corrupt last byte (GCM tag)

        var act = () => sut.Decrypt(ct, dek);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithTamperedEncryptedDekTag_ThrowsCryptographicException()
    {
        var sut = BuildSut();
        var (ct, dek) = sut.Encrypt("secret"u8.ToArray());
        dek[^1] ^= 0xFF; // corrupt DEK's GCM tag

        var act = () => sut.Decrypt(ct, dek);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Constructor_WithMissingConfig_ThrowsInvalidOperationException()
    {
        var act = () => new AesGcmImageEncryptionService(new ConfigurationBuilder().Build());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MasterKeyBase64*");
    }

    [Fact]
    public void Constructor_WithKeyTooShort_ThrowsInvalidOperationException()
    {
        var act = () => BuildSut(Convert.ToBase64String(new byte[16]));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32*");
    }
}
