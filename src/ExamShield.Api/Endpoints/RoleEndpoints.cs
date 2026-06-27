using ExamShield.Api.Contracts;
using ExamShield.Application.RolePermissions;

namespace ExamShield.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/roles", () =>
        {
            var roles = RolePermissionDefinitions.All
                .Select(r => new RoleResponse(
                    r.Role.ToString(),
                    r.DisplayName,
                    r.Description,
                    r.Permissions))
                .ToList();
            return Results.Ok(new RoleListResponse(roles));
        })
        .WithName("GetRoles")
        .WithTags("Roles")
        .RequireAuthorization("Administrator")
        .Produces<RoleListResponse>();

        return app;
    }
}
