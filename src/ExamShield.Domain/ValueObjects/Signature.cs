namespace ExamShield.Domain.ValueObjects;

public sealed class Signature : IEquatable<Signature>
{
    public IReadOnlyList<byte> Bytes { get; }

    public Signature(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
        if (bytes.Length == 0)
            throw new ArgumentException("Signature bytes cannot be empty.", nameof(bytes));
        Bytes = bytes.ToArray();
    }

    public bool Equals(Signature? other) =>
        other is not null && Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => Equals(obj as Signature);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in Bytes) hash.Add(b);
        return hash.ToHashCode();
    }
}
