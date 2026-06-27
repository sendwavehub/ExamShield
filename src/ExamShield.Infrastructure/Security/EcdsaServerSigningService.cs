using System.Security.Cryptography;
using System.Text;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Security;

public sealed class EcdsaServerSigningService : IServerSigningService, IDisposable
{
    private readonly ECDsa _key;

    public EcdsaServerSigningService(string? privateKeyPem)
    {
        _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        if (!string.IsNullOrWhiteSpace(privateKeyPem))
            _key.ImportFromPem(privateKeyPem);
    }

    public string Sign(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(_key.SignHash(hash));
    }

    public bool Verify(string data, string signatureBase64)
    {
        try
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            var sig = Convert.FromBase64String(signatureBase64);
            return _key.VerifyHash(hash, sig);
        }
        catch
        {
            return false;
        }
    }

    public string PublicKeyBase64 =>
        Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());

    public string ExportPrivateKeyPem() => _key.ExportECPrivateKeyPem();

    public void Dispose() => _key.Dispose();
}
