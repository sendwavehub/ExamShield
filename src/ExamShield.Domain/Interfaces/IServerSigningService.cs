namespace ExamShield.Domain.Interfaces;

public interface IServerSigningService
{
    string Sign(string data);
    bool Verify(string data, string signatureBase64);
    string PublicKeyBase64 { get; }
}
