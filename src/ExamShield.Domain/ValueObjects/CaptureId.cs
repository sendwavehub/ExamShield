namespace ExamShield.Domain.ValueObjects;

public sealed record CaptureId : GuidId
{
    public CaptureId(Guid value) : base(value) { }
    public static CaptureId New() => new(Guid.NewGuid());
}
