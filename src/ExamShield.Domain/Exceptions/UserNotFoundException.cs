namespace ExamShield.Domain.Exceptions;

public sealed class UserNotFoundException(Guid userId)
    : Exception($"User '{userId}' was not found.");
