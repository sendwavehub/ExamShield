namespace ExamShield.Domain.ValueObjects;

public sealed record OcrResultId : GuidId
{
    public OcrResultId(Guid value) : base(value) { }
    public static OcrResultId New() => new(Guid.NewGuid());
}
