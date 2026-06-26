namespace ExamShield.Domain.ValueObjects;

public sealed record StudentId : GuidId
{
    public StudentId(Guid value) : base(value) { }
    public static StudentId New() => new(Guid.NewGuid());
}
