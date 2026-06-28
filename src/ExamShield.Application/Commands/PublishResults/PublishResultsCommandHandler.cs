using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.PublishResults;

public sealed class PublishResultsCommandHandler
    : IRequestHandler<PublishResultsCommand, PublishResultsResult>
{
    private readonly IScoreRepository _scores;
    private readonly IAuditLogRepository _auditLog;
    private readonly ICacheService _cache;
    private readonly IExamRepository _exams;

    public PublishResultsCommandHandler(
        IScoreRepository scores, IAuditLogRepository auditLog, ICacheService cache,
        IExamRepository exams)
    {
        _scores = scores;
        _auditLog = auditLog;
        _cache = cache;
        _exams = exams;
    }

    public async Task<PublishResultsResult> Handle(
        PublishResultsCommand command, CancellationToken ct)
    {
        var examId = new ExamId(command.ExamId);

        var exam = await _exams.GetByIdAsync(examId, ct)
            ?? throw new KeyNotFoundException($"Exam '{command.ExamId}' not found.");

        if (exam.Status != ExamStatus.Closed)
            throw new InvalidOperationException(
                $"Results can only be published for a closed exam. Exam '{command.ExamId}' is currently {exam.Status}.");

        var all = await _scores.GetByExamIdAsync(examId, ct);

        if (all.Count == 0)
            throw new NoScoresToPublishException(command.ExamId);

        var unpublished = all.Where(s => !s.IsPublished).ToList();

        if (unpublished.Count == 0)
            throw new ResultsAlreadyPublishedException(command.ExamId);

        foreach (var score in unpublished)
        {
            score.Publish();
            await _scores.UpdateAsync(score, ct);
        }

        if (unpublished.Count > 0)
        {
            await _auditLog.AppendAsync(
                AuditLog.Record(AuditAction.ResultsPublished,
                    reason: $"Published {unpublished.Count} result(s) for exam {command.ExamId}"), ct);
            await _cache.InvalidateAsync(CacheKeys.PublishedResults, ct);
            await _cache.InvalidateAsync(CacheKeys.Statistics, ct);
        }

        return new PublishResultsResult(unpublished.Count);
    }
}
