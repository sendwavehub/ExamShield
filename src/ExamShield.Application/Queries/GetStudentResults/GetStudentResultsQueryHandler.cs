using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetStudentResults;

public sealed class GetStudentResultsQueryHandler(
    ICaptureRepository captures,
    IScoreRepository scores,
    IExamRepository exams)
    : IRequestHandler<GetStudentResultsQuery, GetStudentResultsResult>
{
    public async Task<GetStudentResultsResult> Handle(GetStudentResultsQuery request, CancellationToken ct)
    {
        var studentId = new StudentId(request.StudentId);

        // Scores already carry StudentId — no capture join needed for the score lookup.
        var allScores = await scores.GetAllAsync(ct);
        var studentScores = allScores.Where(s => s.StudentId == studentId).ToList();

        if (studentScores.Count == 0)
            return new GetStudentResultsResult(request.StudentId, []);

        var captureIds = studentScores.Select(s => s.CaptureId).ToHashSet();
        var allCaptures = await captures.ListByStudentIdAsync(studentId, ct);
        var capturesById = allCaptures.ToDictionary(c => c.Id);

        var allExams = await exams.ListAllAsync(ct);
        var examsById = allExams.ToDictionary(e => e.Id);

        var items = studentScores.Select(score =>
        {
            capturesById.TryGetValue(score.CaptureId, out var capture);
            examsById.TryGetValue(score.ExamId, out var exam);
            return new StudentResultItem(
                ScoreId:        score.Id.Value,
                CaptureId:      score.CaptureId.Value,
                ExamId:         score.ExamId.Value,
                ExamName:       exam?.Name ?? score.ExamId.Value.ToString(),
                CorrectAnswers: score.CorrectAnswers,
                TotalQuestions: score.TotalQuestions,
                Percentage:     score.Percentage,
                ScoredAt:       score.ScoredAt,
                HashHex:        capture?.ExpectedHash?.Hex ?? string.Empty,
                IsVerified:     capture?.Status == CaptureStatus.Verified);
        }).ToList();

        return new GetStudentResultsResult(request.StudentId, items);
    }
}
