using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.UpdateSettings;
using ExamShield.Application.Queries.GetSettings;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings").WithTags("Settings");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetSettingsQuery(), ct);
            return Results.Ok(ToResponse(dto));
        })
        .WithName("GetSettings")
        .RequireAuthorization("Administrator")
        .Produces<SettingsResponse>();

        group.MapPut("/", async (UpdateSettingsRequest request, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new UpdateSettingsCommand(
                request.OcrConfidenceThreshold, request.NotificationsEnabled,
                request.NotificationSeverity, request.AccessTokenExpiryMinutes,
                request.RefreshTokenExpiryDays), ct);
            return Results.Ok(ToResponse(dto));
        })
        .WithName("UpdateSettings")
        .RequireAuthorization("Administrator")
        .Produces<SettingsResponse>()
        .ProducesValidationProblem();

        return app;
    }

    private static SettingsResponse ToResponse(SettingsDto dto) =>
        new(dto.OcrConfidenceThreshold, dto.NotificationsEnabled,
            dto.NotificationSeverity, dto.AccessTokenExpiryMinutes, dto.RefreshTokenExpiryDays);
}
