namespace ExamShield.Domain.ValueObjects;

public sealed record ExamId : GuidId
{
    public ExamId(Guid value) : base(value) { }
    public static ExamId New() => new(Guid.NewGuid());
}
