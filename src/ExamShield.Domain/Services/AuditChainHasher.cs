using System.Security.Cryptography;
using System.Text;
using ExamShield.Domain.Entities;

namespace ExamShield.Domain.Services;

public static class AuditChainHasher
{
    public static string ComputeContentHash(AuditLog entry, string previousHash)
    {
        var data = $"{entry.Action}|{entry.CaptureId?.Value}|{entry.OccurredAt.UtcTicks}|{previousHash}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
