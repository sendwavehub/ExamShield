namespace ExamShield.Api.Contracts;

public sealed record UserResponse(
    Guid UserId, string Email, string Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record UserListResponse(IReadOnlyList<UserResponse> Users);

public sealed record UpdateUserRoleRequest(string Role);
