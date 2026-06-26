using System.Security.Cryptography;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Services;

public sealed class HashVerificationService
{
    public Hash ComputeHash(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes, nameof(imageBytes));
        if (imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

        return Hash.FromBytes(SHA256.HashData(imageBytes));
    }

    public bool Verify(byte[] imageBytes, Hash expectedHash)
    {
        ArgumentNullException.ThrowIfNull(imageBytes, nameof(imageBytes));
        ArgumentNullException.ThrowIfNull(expectedHash, nameof(expectedHash));

        return ComputeHash(imageBytes) == expectedHash;
    }
}
