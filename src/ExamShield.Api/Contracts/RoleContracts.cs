namespace ExamShield.Api.Contracts;

public sealed record RoleResponse(
    string RoleName,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Permissions);

public sealed record RoleListResponse(IReadOnlyList<RoleResponse> Roles);
