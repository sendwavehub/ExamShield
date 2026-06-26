using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.PublishResults;

public sealed class PublishResultsCommandHandler
    : IRequestHandler<PublishResultsCommand, PublishResultsResult>
{
    private readonly IScoreRepository _scores;
    private readonly IAuditLogRepository _auditLog;

    public PublishResultsCommandHandler(IScoreRepository scores, IAuditLogRepository auditLog)
    {
        _scores = scores;
        _auditLog = auditLog;
    }

    public async Task<PublishResultsResult> Handle(
        PublishResultsCommand command, CancellationToken ct)
    {
        var examId = new ExamId(command.ExamId);
        var all = await _scores.GetByExamIdAsync(examId, ct);
        var unpublished = all.Where(s => !s.IsPublished).ToList();

        foreach (var score in unpublished)
        {
            score.Publish();
            await _scores.UpdateAsync(score, ct);
        }

        if (unpublished.Count > 0)
            await _auditLog.AppendAsync(
                AuditLog.Record(AuditAction.ResultsPublished,
                    reason: $"Published {unpublished.Count} result(s) for exam {command.ExamId}"), ct);

        return new PublishResultsResult(unpublished.Count);
    }
}
