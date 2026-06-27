using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.BulkEnrollStudents;

public sealed record BulkEnrollStudentsResult(int Enrolled, int AlreadyEnrolled)
{
    public int Total => Enrolled + AlreadyEnrolled;
}

public sealed record BulkEnrollStudentsCommand(
    Guid ExamId,
    IReadOnlyList<Guid> StudentIds) : IRequest<BulkEnrollStudentsResult>;

public sealed class BulkEnrollStudentsCommandHandler(
    IExamRepository exams,
    IExamCandidateRepository candidates,
    IAuditLogRepository audit) : IRequestHandler<BulkEnrollStudentsCommand, BulkEnrollStudentsResult>
{
    public async Task<BulkEnrollStudentsResult> Handle(
        BulkEnrollStudentsCommand command, CancellationToken ct)
    {
        var exam = await exams.GetByIdAsync(new ExamId(command.ExamId), ct)
            ?? throw new KeyNotFoundException($"Exam {command.ExamId} not found.");

        int enrolled = 0, skipped = 0;

        foreach (var id in command.StudentIds)
        {
            var studentId = new StudentId(id);
            if (await candidates.ExistsAsync(exam.Id, studentId, ct))
            {
                skipped++;
                continue;
            }
            await candidates.AddAsync(ExamCandidate.Enroll(exam.Id, studentId), ct);
            enrolled++;
        }

        if (enrolled > 0)
            await audit.AppendAsync(
                AuditLog.Record(AuditAction.StudentEnrolled,
                    reason: $"{enrolled} students bulk-enrolled in exam {command.ExamId}"), ct);

        return new BulkEnrollStudentsResult(enrolled, skipped);
    }
}
