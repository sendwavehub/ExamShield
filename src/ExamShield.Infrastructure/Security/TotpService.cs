using System.Security.Cryptography;
using ExamShield.Application.Interfaces;

namespace ExamShield.Infrastructure.Security;

public sealed class TotpService : ITotpService
{
    private const int SecretByteLength = 20;
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;
    private static readonly int Modulus = (int)Math.Pow(10, Digits);

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretByteLength);
        return Base32Encode(bytes);
    }

    public string GetQrUri(string secret, string email, string issuer = "ExamShield")
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}"
             + $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }

    public bool Verify(string base32Secret, string code)
    {
        if (code.Length != Digits || !code.All(char.IsAsciiDigit)) return false;
        var key = Base32Decode(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;
        return new[] { -1L, 0L, 1L }.Any(offset =>
            ComputeTotp(key, counter + offset) == code);
    }

    public string GenerateCurrentCode(string base32Secret)
    {
        var key = Base32Decode(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;
        return ComputeTotp(key, counter);
    }

    private static string ComputeTotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24 |
                    (hash[offset + 1] & 0xFF) << 16 |
                    (hash[offset + 2] & 0xFF) << 8 |
                    (hash[offset + 3] & 0xFF)) % Modulus;
        return code.ToString($"D{Digits}");
    }

    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    private static string Base32Encode(byte[] data)
    {
        var result = new char[(data.Length * 8 + 4) / 5];
        int buffer = data[0], bitsLeft = 8, index = 0, i = 1;
        while (bitsLeft > 0 || i < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (i < data.Length) { buffer = (buffer << 8) | data[i++]; bitsLeft += 8; }
                else { buffer <<= 5 - bitsLeft; bitsLeft = 5; }
            }
            bitsLeft -= 5;
            result[index++] = Base32Alphabet[(buffer >> bitsLeft) & 0x1F];
        }
        return new string(result, 0, index);
    }

    private static byte[] Base32Decode(string base32)
    {
        var upper = base32.ToUpperInvariant().TrimEnd('=');
        var result = new byte[upper.Length * 5 / 8];
        int buffer = 0, bitsLeft = 0, index = 0;
        foreach (var c in upper)
        {
            var val = Array.IndexOf(Base32Alphabet, c);
            if (val < 0) continue;
            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8) { result[index++] = (byte)(buffer >> (bitsLeft -= 8)); }
        }
        return result;
    }
}
