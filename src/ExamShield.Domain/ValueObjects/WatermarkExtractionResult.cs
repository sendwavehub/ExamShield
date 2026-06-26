namespace ExamShield.Domain.ValueObjects;

public sealed class WatermarkExtractionResult
{
    public bool IsValid { get; private init; }
    public WatermarkPayload? Payload { get; private init; }
    public int OriginalImageLength { get; private init; }

    public static WatermarkExtractionResult Success(WatermarkPayload payload, int originalImageLength) =>
        new() { IsValid = true, Payload = payload, OriginalImageLength = originalImageLength };

    public static WatermarkExtractionResult Failure() =>
        new() { IsValid = false };
}
