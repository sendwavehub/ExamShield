namespace ExamShield.Domain.Exceptions;

public sealed class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists.")
    { }
}
