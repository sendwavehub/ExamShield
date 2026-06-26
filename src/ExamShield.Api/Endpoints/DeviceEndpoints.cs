using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.RegisterDevice;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/devices", RegisterDeviceAsync)
            .WithName("RegisterDevice")
            .WithTags("Device")
            .RequireAuthorization("Administrator")
            .Produces<RegisterDeviceResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> RegisterDeviceAsync(
        RegisterDeviceRequest request, ISender sender, CancellationToken ct)
    {
        var command = new RegisterDeviceCommand(request.Name, request.PublicKeyBytes);
        var result = await sender.Send(command, ct);

        return Results.Created(
            $"/devices/{result.DeviceId}",
            new RegisterDeviceResponse(result.DeviceId));
    }
}
