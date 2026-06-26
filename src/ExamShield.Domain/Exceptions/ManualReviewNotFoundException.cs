namespace ExamShield.Domain.Exceptions;

public sealed class ManualReviewNotFoundException : Exception
{
    public ManualReviewNotFoundException(Guid reviewId)
        : base($"Manual review '{reviewId}' was not found.") { }
}
