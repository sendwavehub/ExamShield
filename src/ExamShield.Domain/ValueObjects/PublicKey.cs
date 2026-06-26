namespace ExamShield.Domain.ValueObjects;

public sealed class PublicKey : IEquatable<PublicKey>
{
    public IReadOnlyList<byte> Bytes { get; }

    public PublicKey(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
        if (bytes.Length == 0)
            throw new ArgumentException("Public key bytes cannot be empty.", nameof(bytes));
        Bytes = bytes.ToArray(); // defensive copy
    }

    public bool Equals(PublicKey? other) =>
        other is not null && Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => Equals(obj as PublicKey);

    public override int GetHashCode()
    {
        var h = new HashCode();
        foreach (var b in Bytes) h.Add(b);
        return h.ToHashCode();
    }
}
