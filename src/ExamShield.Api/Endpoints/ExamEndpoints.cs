using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.CreateExam;
using ExamShield.Application.Queries.GetExams;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class ExamEndpoints
{
    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/exams").WithTags("Exams");

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetExamsQuery(), ct);
            var items = result.Exams
                .Select(e => new ExamResponse(e.ExamId, e.Name, e.Description, e.Status, e.TotalQuestions, e.CreatedAt))
                .ToList();
            return Results.Ok(new ExamListResponse(items));
        })
        .WithName("GetExams")
        .RequireAuthorization("Operator")
        .Produces<ExamListResponse>();

        group.MapPost("/", async (CreateExamRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CreateExamCommand(request.Name, request.Description, request.TotalQuestions), ct);
            var response = new ExamResponse(
                result.ExamId, result.Name, result.Description,
                result.Status, result.TotalQuestions, result.CreatedAt);
            return Results.Created($"/exams/{result.ExamId}", response);
        })
        .WithName("CreateExam")
        .RequireAuthorization("Administrator")
        .Produces<ExamResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
