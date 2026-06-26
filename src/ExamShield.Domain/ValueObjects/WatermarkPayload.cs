namespace ExamShield.Domain.ValueObjects;

public sealed record WatermarkPayload
{
    public Guid ExamId { get; init; }
    public Guid CaptureId { get; init; }
    public long TimestampUtcTicks { get; init; }
    public string Nonce { get; init; } = "";
    public string ImageHash { get; init; } = "";
}
