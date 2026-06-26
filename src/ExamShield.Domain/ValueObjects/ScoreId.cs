namespace ExamShield.Domain.ValueObjects;

public sealed record ScoreId : GuidId
{
    public ScoreId(Guid value) : base(value) { }
    public static ScoreId New() => new(Guid.NewGuid());
}
