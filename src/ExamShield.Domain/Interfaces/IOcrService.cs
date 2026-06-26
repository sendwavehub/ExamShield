using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public sealed class OcrExtractionResult
{
    public IReadOnlyList<ExtractedAnswer> Answers { get; }
    public OcrExtractionResult(IReadOnlyList<ExtractedAnswer> answers) => Answers = answers;
}

public interface IOcrService
{
    Task<OcrExtractionResult> ExtractAsync(byte[] imageBytes, CancellationToken ct = default);
}
