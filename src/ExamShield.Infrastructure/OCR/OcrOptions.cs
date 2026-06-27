namespace ExamShield.Infrastructure.OCR;

public sealed class OcrOptions
{
    public const string Section = "Ocr";
    public string Type { get; init; } = "Stub";
    public string Endpoint { get; init; } = "/ocr/extract";
    public int TimeoutSeconds { get; init; } = 30;
}
