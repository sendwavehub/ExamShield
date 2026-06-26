using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetOcrResult;

public sealed class GetOcrResultQueryHandler : IRequestHandler<GetOcrResultQuery, GetOcrResultResult>
{
    private readonly IOcrResultRepository _ocrResults;

    public GetOcrResultQueryHandler(IOcrResultRepository ocrResults) => _ocrResults = ocrResults;

    public async Task<GetOcrResultResult> Handle(GetOcrResultQuery query, CancellationToken ct)
    {
        var result = await _ocrResults.GetByCaptureIdAsync(new CaptureId(query.CaptureId), ct)
            ?? throw new OcrResultNotFoundException(query.CaptureId);

        return new GetOcrResultResult(
            result.Id.Value,
            result.CaptureId.Value,
            result.Status,
            result.OverallConfidence.Value,
            result.RequiresManualReview,
            result.Answers.Select(a => new OcrAnswerDto(a.QuestionNumber, a.Text, a.Confidence.Value)).ToList());
    }
}
