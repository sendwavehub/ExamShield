using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class ReviewRequest : AggregateRoot
{
    public ReviewRequestId Id { get; private set; } = null!;
    public StudentId StudentId { get; private set; } = null!;
    public CaptureId CaptureId { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public ReviewRequestStatus Status { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ReviewRequest() { }

    public void Resolve(string note)
    {
        if (Status is ReviewRequestStatus.Resolved or ReviewRequestStatus.Rejected)
            throw new InvalidOperationException("Review request has already been closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(note, nameof(note));
        Status = ReviewRequestStatus.Resolved;
        ResolutionNote = note.Trim();
    }

    public void Reject(string reason)
    {
        if (Status is ReviewRequestStatus.Resolved or ReviewRequestStatus.Rejected)
            throw new InvalidOperationException("Review request has already been closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = ReviewRequestStatus.Rejected;
        ResolutionNote = reason.Trim();
    }

    public static ReviewRequest Submit(StudentId studentId, CaptureId captureId, string reason)
    {
        ArgumentNullException.ThrowIfNull(studentId);
        ArgumentNullException.ThrowIfNull(captureId);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty.", nameof(reason));

        return new ReviewRequest
        {
            Id = ReviewRequestId.New(),
            StudentId = studentId,
            CaptureId = captureId,
            Reason = reason.Trim(),
            Status = ReviewRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
