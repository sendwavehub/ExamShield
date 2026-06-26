using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.CreateUser;
using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace ExamShield.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/users", CreateUserAsync)
            .WithName("CreateUser")
            .RequireAuthorization("Administrator")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return Results.Ok(new LoginResponse(result.Token, result.Role));
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request, ISender sender, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{request.Role}'.", nameof(request));

        var result = await sender.Send(new CreateUserCommand(request.Email, request.Password, role), ct);
        return Results.Created($"/auth/users/{result.UserId}", new CreateUserResponse(result.UserId));
    }
}
