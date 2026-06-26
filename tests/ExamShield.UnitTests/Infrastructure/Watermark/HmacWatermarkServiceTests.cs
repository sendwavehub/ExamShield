using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Watermark;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Watermark;

public sealed class HmacWatermarkServiceTests
{
    private static readonly byte[] Key = new byte[32];
    private readonly HmacWatermarkService _sut = new(Key);

    private static readonly byte[] ImageBytes = [0x01, 0x02, 0x03, 0x04, 0x05,
        0x06, 0x07, 0x08, 0x09, 0x0A];

    private static WatermarkPayload BuildPayload(byte[] image) => new()
    {
        ExamId = Guid.NewGuid(),
        CaptureId = Guid.NewGuid(),
        TimestampUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
        Nonce = "abc123",
        ImageHash = "aabbcc"
    };

    [Fact]
    public void Embed_ReturnsBytesLongerThanOriginal()
    {
        var payload = BuildPayload(ImageBytes);

        var result = _sut.Embed(ImageBytes, payload);

        result.Length.Should().BeGreaterThan(ImageBytes.Length);
    }

    [Fact]
    public void Embed_OriginalBytesPreservedAtStart()
    {
        var payload = BuildPayload(ImageBytes);

        var result = _sut.Embed(ImageBytes, payload);

        result[..ImageBytes.Length].Should().Equal(ImageBytes);
    }

    [Fact]
    public void Extract_AfterEmbed_ReturnsOriginalPayload()
    {
        var payload = BuildPayload(ImageBytes);
        var watermarked = _sut.Embed(ImageBytes, payload);

        var result = _sut.Extract(watermarked);

        result.IsValid.Should().BeTrue();
        result.Payload!.ExamId.Should().Be(payload.ExamId);
        result.Payload.CaptureId.Should().Be(payload.CaptureId);
        result.Payload.ImageHash.Should().Be(payload.ImageHash);
    }

    [Fact]
    public void Extract_ReturnsCorrectOriginalImageLength()
    {
        var payload = BuildPayload(ImageBytes);
        var watermarked = _sut.Embed(ImageBytes, payload);

        var result = _sut.Extract(watermarked);

        result.OriginalImageLength.Should().Be(ImageBytes.Length);
    }

    [Fact]
    public void Extract_WhenPayloadTampered_ReturnsFailure()
    {
        var watermarked = _sut.Embed(ImageBytes, BuildPayload(ImageBytes));
        // Corrupt a byte in the payload area (after image, before the end)
        watermarked[ImageBytes.Length + 10] ^= 0xFF;

        var result = _sut.Extract(watermarked);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Extract_WhenBytesTooShort_ReturnsFailure()
    {
        var result = _sut.Extract([0x01, 0x02]);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Extract_WhenNoWatermark_ReturnsFailure()
    {
        var result = _sut.Extract(ImageBytes);

        result.IsValid.Should().BeFalse();
    }
}
