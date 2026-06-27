using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public enum ExamStatus { Draft, Active, Closed }

public sealed class Exam : AggregateRoot
{
    public ExamId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ExamStatus Status { get; private set; }
    public int TotalQuestions { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Exam() { }

    private Exam(ExamId id, string name, string? description, int totalQuestions, DateTimeOffset createdAt)
    {
        Id = id; Name = name; Description = description;
        TotalQuestions = totalQuestions; CreatedAt = createdAt;
        Status = ExamStatus.Draft;
    }

    public static Exam Create(string name, string? description, int totalQuestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (totalQuestions <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalQuestions), "Must be greater than zero.");

        return new Exam(ExamId.New(), name, description, totalQuestions, DateTimeOffset.UtcNow);
    }

    public void Activate()
    {
        if (Status != ExamStatus.Draft)
            throw new InvalidOperationException("Only Draft exams can be activated.");
        Status = ExamStatus.Active;
    }

    public void Close()
    {
        if (Status == ExamStatus.Closed)
            throw new InvalidOperationException("Exam is already closed.");
        Status = ExamStatus.Closed;
    }
}
