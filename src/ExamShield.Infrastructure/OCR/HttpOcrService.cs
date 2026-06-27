using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Infrastructure.OCR;

public sealed class HttpOcrService(HttpClient httpClient, OcrOptions options) : IOcrService
{
    public async Task<OcrExtractionResult> ExtractAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        using var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await httpClient.PostAsync(options.Endpoint, content, ct);
        response.EnsureSuccessStatusCode();

        var ocrResponse = await response.Content.ReadFromJsonAsync<OcrHttpResponse>(ct)
            ?? throw new InvalidOperationException("OCR service returned empty response.");

        var answers = ocrResponse.Answers
            .Select(a => new ExtractedAnswer(
                a.QuestionNumber,
                a.SelectedOption,
                new OcrConfidence(a.Confidence)))
            .ToList();

        return new OcrExtractionResult(answers);
    }

    private sealed record OcrHttpResponse(IReadOnlyList<OcrAnswerDto> Answers);
    private sealed record OcrAnswerDto(int QuestionNumber, string SelectedOption, double Confidence);
}
