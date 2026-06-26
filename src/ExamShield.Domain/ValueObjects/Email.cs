using System.Text.RegularExpressions;

namespace ExamShield.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex _pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(address));
        if (!_pattern.IsMatch(address))
            throw new ArgumentException($"'{address}' is not a valid email address.", nameof(address));
        Value = address.ToLowerInvariant();
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Email);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;
}
