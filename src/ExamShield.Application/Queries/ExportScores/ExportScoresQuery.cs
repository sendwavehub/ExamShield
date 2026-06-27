using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;
using System.Text;

namespace ExamShield.Application.Queries.ExportScores;

public sealed record ExportScoresResult(string Csv, string Filename);
public sealed record ExportScoresQuery(Guid? ExamId = null) : IRequest<ExportScoresResult>;

public sealed class ExportScoresQueryHandler(IScoreRepository scores)
    : IRequestHandler<ExportScoresQuery, ExportScoresResult>
{
    public async Task<ExportScoresResult> Handle(ExportScoresQuery query, CancellationToken ct)
    {
        IReadOnlyList<Score> rows = query.ExamId.HasValue
            ? await scores.GetByExamIdAsync(new ExamId(query.ExamId.Value), ct)
            : await scores.GetAllAsync(ct);

        var csv = new StringBuilder();
        csv.AppendLine("ScoreId,ExamId,StudentId,CaptureId,CorrectAnswers,TotalQuestions,Percentage,ScoredAt");

        foreach (var s in rows)
        {
            csv.Append(Escape(s.Id.Value.ToString())).Append(',');
            csv.Append(Escape(s.ExamId.Value.ToString())).Append(',');
            csv.Append(Escape(s.StudentId.Value.ToString())).Append(',');
            csv.Append(Escape(s.CaptureId.Value.ToString())).Append(',');
            csv.Append(s.CorrectAnswers).Append(',');
            csv.Append(s.TotalQuestions).Append(',');
            csv.Append(s.Percentage.ToString("F2")).Append(',');
            csv.AppendLine(Escape(s.ScoredAt.ToString("O")));
        }

        var filename = $"scores-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return new ExportScoresResult(csv.ToString(), filename);
    }

    private static string Escape(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
