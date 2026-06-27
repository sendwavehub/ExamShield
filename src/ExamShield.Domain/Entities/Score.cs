using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class Score : AggregateRoot
{
    public ScoreId Id { get; private set; } = null!;
    public CaptureId CaptureId { get; private set; } = null!;
    public ExamId ExamId { get; private set; } = null!;
    public StudentId StudentId { get; private set; } = null!;
    public int CorrectAnswers { get; private set; }
    public int TotalQuestions { get; private set; }
    public double Percentage { get; private set; }
    public DateTimeOffset ScoredAt { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private Score() { }

    public static Score Create(
        CaptureId captureId, ExamId examId, StudentId studentId,
        IReadOnlyList<ExtractedAnswer> answers, AnswerKey answerKey,
        DateTimeOffset? scoredAt = null)
    {
        ArgumentNullException.ThrowIfNull(captureId);
        ArgumentNullException.ThrowIfNull(examId);
        ArgumentNullException.ThrowIfNull(studentId);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(answerKey);

        var correct = answers.Count(a => answerKey.IsCorrect(a.QuestionNumber, a.Text));
        var total = answerKey.Count;
        var pct = total > 0 ? Math.Round((double)correct / total * 100, 2) : 0.0;

        return new Score
        {
            Id = ScoreId.New(),
            CaptureId = captureId,
            ExamId = examId,
            StudentId = studentId,
            CorrectAnswers = correct,
            TotalQuestions = total,
            Percentage = pct,
            ScoredAt = scoredAt ?? DateTimeOffset.UtcNow
        };
    }

    public void Publish()
    {
        if (IsPublished)
            return; // idempotent — already published
        IsPublished = true;
        PublishedAt = DateTimeOffset.UtcNow;
    }
}
