using System.Text.RegularExpressions;

namespace ExamShield.Domain.ValueObjects;

public sealed record Hash
{
    private static readonly Regex HexPattern = new("^[0-9a-f]{64}$", RegexOptions.Compiled);

    public string Hex { get; }

    private Hash(string hex) => Hex = hex;

    public static Hash FromHex(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex, nameof(hex));
        var lower = hex.ToLowerInvariant();
        if (!HexPattern.IsMatch(lower))
            throw new ArgumentException("Hash must be a 64-character hex string (SHA-256).", nameof(hex));
        return new Hash(lower);
    }

    public static Hash FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length != 32)
            throw new ArgumentException("Hash bytes must be exactly 32 bytes (SHA-256).", nameof(bytes));
        return new Hash(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public byte[] ToBytes() => Convert.FromHexString(Hex);

    public override string ToString() => Hex;
}
