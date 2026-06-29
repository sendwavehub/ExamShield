using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

/// <summary>
/// Passes image bytes through unchanged so integration tests can use arbitrary byte arrays.
/// Watermark correctness is verified by LsbSteganographyServiceTests in the unit test suite.
/// </summary>
public sealed class NullWatermarkService : IWatermarkService
{
    public byte[] Embed(byte[] imageBytes, WatermarkPayload payload) => imageBytes;

    public WatermarkExtractionResult Extract(byte[] imageBytes) =>
        WatermarkExtractionResult.Success(new WatermarkPayload(), imageBytes.Length);
}
