using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.DisableDevice;
using ExamShield.Application.Commands.EnableDevice;
using ExamShield.Application.Commands.RegisterDevice;
using ExamShield.Application.Queries.GetDevices;
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

        app.MapGet("/devices", ListDevicesAsync)
            .WithName("ListDevices")
            .WithTags("Device")
            .RequireAuthorization("Administrator")
            .Produces<DeviceListResponse>();

        app.MapPut("/devices/{id:guid}/disable", DisableDeviceAsync)
            .WithName("DisableDevice")
            .WithTags("Device")
            .RequireAuthorization("Administrator")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPut("/devices/{id:guid}/enable", EnableDeviceAsync)
            .WithName("EnableDevice")
            .WithTags("Device")
            .RequireAuthorization("Administrator")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> RegisterDeviceAsync(
        RegisterDeviceRequest request, ISender sender, CancellationToken ct)
    {
        var command = new RegisterDeviceCommand(request.Name, request.PublicKeyBytes);
        var result = await sender.Send(command, ct);
        return Results.Created($"/devices/{result.DeviceId}", new RegisterDeviceResponse(result.DeviceId));
    }

    private static async Task<IResult> ListDevicesAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDevicesQuery(), ct);
        var response = new DeviceListResponse(
            result.Devices.Select(d => new DeviceResponse(d.DeviceId, d.Name, d.IsActive, d.RegisteredAt)).ToList());
        return Results.Ok(response);
    }

    private static async Task<IResult> DisableDeviceAsync(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DisableDeviceCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> EnableDeviceAsync(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new EnableDeviceCommand(id), ct);
        return Results.NoContent();
    }
}
