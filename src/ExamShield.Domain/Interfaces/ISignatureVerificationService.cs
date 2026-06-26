using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface ISignatureVerificationService
{
    bool Verify(Hash hash, Signature signature, PublicKey publicKey);
}
