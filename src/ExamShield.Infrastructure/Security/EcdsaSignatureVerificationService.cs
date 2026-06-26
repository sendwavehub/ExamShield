using System.Security.Cryptography;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Infrastructure.Security;

public sealed class EcdsaSignatureVerificationService : ISignatureVerificationService
{
    public bool Verify(Hash hash, Signature signature, PublicKey publicKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey.Bytes.ToArray(), out _);
            return ecdsa.VerifyHash(hash.ToBytes(), signature.Bytes.ToArray());
        }
        catch (CryptographicException)
        {
            return false; // invalid key or signature format
        }
    }
}
