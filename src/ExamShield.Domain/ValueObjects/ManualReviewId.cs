namespace ExamShield.Domain.ValueObjects;

public sealed record ManualReviewId : GuidId
{
    public ManualReviewId(Guid value) : base(value) { }
    public static ManualReviewId New() => new(Guid.NewGuid());
}
