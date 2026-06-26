namespace ExamShield.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, string Role);

public sealed record CreateUserRequest(string Email, string Password, string Role);

public sealed record CreateUserResponse(Guid UserId);
