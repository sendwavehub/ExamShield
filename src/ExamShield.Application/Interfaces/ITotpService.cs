namespace ExamShield.Application.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrUri(string secret, string email, string issuer = "ExamShield");
    bool Verify(string base32Secret, string code);
    string GenerateCurrentCode(string base32Secret);
}
