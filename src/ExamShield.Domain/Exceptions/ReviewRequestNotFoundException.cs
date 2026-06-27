namespace ExamShield.Domain.Exceptions;

public sealed class ReviewRequestNotFoundException(Guid id)
    : Exception($"Review request '{id}' was not found.");
