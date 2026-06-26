using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface IAnswerKeyRepository
{
    Task<AnswerKey?> GetByExamIdAsync(ExamId examId, CancellationToken ct = default);
}
