namespace ExamShield.Infrastructure.Watermark;

public sealed class WatermarkOptions
{
    public const string Section = "Watermark";
    // Base64-encoded 32-byte HMAC key — rotate regularly
    public string HmacKeyBase64 { get; init; } = string.Empty;
}
