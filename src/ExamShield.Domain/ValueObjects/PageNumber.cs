namespace ExamShield.Domain.ValueObjects;

public sealed record PageNumber
{
    public int Value { get; }

    public PageNumber(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Page number must be positive.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
