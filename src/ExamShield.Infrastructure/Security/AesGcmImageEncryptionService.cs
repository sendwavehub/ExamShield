using System.Security.Cryptography;
using ExamShield.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ExamShield.Infrastructure.Security;

public sealed class AesGcmImageEncryptionService : IImageEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _masterKey;

    public AesGcmImageEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:MasterKeyBase64"]
            ?? throw new InvalidOperationException("Encryption:MasterKeyBase64 is not configured.");
        _masterKey = Convert.FromBase64String(keyBase64);
        if (_masterKey.Length != KeySize)
            throw new InvalidOperationException(
                $"Encryption master key must be {KeySize} bytes, got {_masterKey.Length}. " +
                "Generate one with: openssl rand -base64 32");
    }

    public (byte[] Ciphertext, byte[] EncryptedDek) Encrypt(byte[] plaintext)
    {
        var dek = RandomNumberGenerator.GetBytes(KeySize);

        // Encrypt plaintext with the per-image DEK
        var imageNonce = RandomNumberGenerator.GetBytes(NonceSize);
        var imageCipher = new byte[plaintext.Length];
        var imageTag = new byte[TagSize];
        using (var aes = new AesGcm(dek, TagSize))
            aes.Encrypt(imageNonce, plaintext, imageCipher, imageTag);

        // Layout: nonce(12) || ciphertext(n) || tag(16)
        var ciphertext = new byte[NonceSize + imageCipher.Length + TagSize];
        imageNonce.CopyTo(ciphertext, 0);
        imageCipher.CopyTo(ciphertext, NonceSize);
        imageTag.CopyTo(ciphertext, NonceSize + imageCipher.Length);

        // Wrap the DEK with the master key (also AES-256-GCM — authenticated)
        var dekNonce = RandomNumberGenerator.GetBytes(NonceSize);
        var dekCipher = new byte[KeySize];
        var dekTag = new byte[TagSize];
        using (var aes = new AesGcm(_masterKey, TagSize))
            aes.Encrypt(dekNonce, dek, dekCipher, dekTag);

        // Layout: nonce(12) || encryptedDek(32) || tag(16) — total 60 bytes
        var encryptedDek = new byte[NonceSize + KeySize + TagSize];
        dekNonce.CopyTo(encryptedDek, 0);
        dekCipher.CopyTo(encryptedDek, NonceSize);
        dekTag.CopyTo(encryptedDek, NonceSize + KeySize);

        return (ciphertext, encryptedDek);
    }

    public byte[] Decrypt(byte[] ciphertext, byte[] encryptedDek)
    {
        // Unwrap DEK with master key
        var dekNonce = encryptedDek[..NonceSize];
        var dekCipher = encryptedDek[NonceSize..(NonceSize + KeySize)];
        var dekTag = encryptedDek[(NonceSize + KeySize)..];
        var dek = new byte[KeySize];
        using (var aes = new AesGcm(_masterKey, TagSize))
            aes.Decrypt(dekNonce, dekCipher, dekTag, dek);

        // Decrypt image ciphertext
        var imageNonce = ciphertext[..NonceSize];
        var imageCipher = ciphertext[NonceSize..(ciphertext.Length - TagSize)];
        var imageTag = ciphertext[(ciphertext.Length - TagSize)..];
        var plaintext = new byte[imageCipher.Length];
        using (var aes = new AesGcm(dek, TagSize))
            aes.Decrypt(imageNonce, imageCipher, imageTag, plaintext);

        return plaintext;
    }
}
