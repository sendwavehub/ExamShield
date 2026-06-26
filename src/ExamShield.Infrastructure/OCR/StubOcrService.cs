using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Infrastructure.OCR;

public sealed class StubOcrService : IOcrService
{
    public Task<OcrExtractionResult> ExtractAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        var answers = new[]
        {
            new ExtractedAnswer(1, "A", new OcrConfidence(0.95)),
            new ExtractedAnswer(2, "B", new OcrConfidence(0.90)),
            new ExtractedAnswer(3, "C", new OcrConfidence(0.88))
        };
        return Task.FromResult(new OcrExtractionResult(answers));
    }
}
