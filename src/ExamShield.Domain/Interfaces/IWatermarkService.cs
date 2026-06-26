using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface IWatermarkService
{
    byte[] Embed(byte[] imageBytes, WatermarkPayload payload);
    WatermarkExtractionResult Extract(byte[] imageBytes);
}
