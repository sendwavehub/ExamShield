using System.Security.Cryptography;
using System.Text.Json;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Infrastructure.Watermark;

// Envelope appended after image bytes:
// [MAGIC (8)][payload JSON (variable)][payloadLength (4, little-endian)][HMAC-SHA256 (32)]
// Length is last fixed-width field before HMAC so it can be read without knowing payload size.
public sealed class HmacWatermarkService : IWatermarkService, IDisposable
{
    private static readonly byte[] Magic = "WMK_ES01"u8.ToArray();
    private const int MagicSize = 8;
    private const int LengthSize = 4;
    private const int HmacSize = 32;
    private const int MinEnvelopeSize = MagicSize + 1 + LengthSize + HmacSize;

    private readonly HMACSHA256 _hmac;

    public HmacWatermarkService(byte[] key) => _hmac = new HMACSHA256(key);

    public byte[] Embed(byte[] imageBytes, WatermarkPayload payload)
    {
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(payload);
        var mac = _hmac.ComputeHash(payloadJson);

        // Envelope: [MAGIC][payloadJson][payloadLen (4)][HMAC (32)]
        var envelope = new byte[MagicSize + payloadJson.Length + LengthSize + HmacSize];
        Magic.CopyTo(envelope, 0);
        payloadJson.CopyTo(envelope, MagicSize);
        BitConverter.GetBytes(payloadJson.Length).CopyTo(envelope, MagicSize + payloadJson.Length);
        mac.CopyTo(envelope, MagicSize + payloadJson.Length + LengthSize);

        return [..imageBytes, ..envelope];
    }

    public WatermarkExtractionResult Extract(byte[] imageBytes)
    {
        try
        {
            var len = imageBytes.Length;
            if (len < MinEnvelopeSize) return WatermarkExtractionResult.Failure();

            // Read from end: HMAC (32) → payloadLen (4) → payload (var) → MAGIC (8)
            var mac = imageBytes[(len - HmacSize)..];
            var payloadLen = BitConverter.ToInt32(imageBytes, len - HmacSize - LengthSize);

            if (payloadLen <= 0 || payloadLen > len - MagicSize - LengthSize - HmacSize)
                return WatermarkExtractionResult.Failure();

            var payloadOffset = len - HmacSize - LengthSize - payloadLen;
            var magicOffset = payloadOffset - MagicSize;

            if (magicOffset < 0) return WatermarkExtractionResult.Failure();

            if (!imageBytes[magicOffset..(magicOffset + MagicSize)].SequenceEqual(Magic))
                return WatermarkExtractionResult.Failure();

            var payloadJson = imageBytes[payloadOffset..(payloadOffset + payloadLen)];
            var expectedMac = _hmac.ComputeHash(payloadJson);
            if (!mac.SequenceEqual(expectedMac))
                return WatermarkExtractionResult.Failure();

            var payload = JsonSerializer.Deserialize<WatermarkPayload>(payloadJson);
            if (payload is null) return WatermarkExtractionResult.Failure();

            return WatermarkExtractionResult.Success(payload, originalImageLength: magicOffset);
        }
        catch
        {
            return WatermarkExtractionResult.Failure();
        }
    }

    public void Dispose() => _hmac.Dispose();
}
