namespace ExamShield.Domain.Exceptions;

public sealed class InvalidResetTokenException()
    : Exception("The password reset token is invalid, expired, or has already been used.");
