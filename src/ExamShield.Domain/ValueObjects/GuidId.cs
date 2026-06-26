namespace ExamShield.Domain.ValueObjects;

public abstract record GuidId
{
    public Guid Value { get; }

    protected GuidId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
