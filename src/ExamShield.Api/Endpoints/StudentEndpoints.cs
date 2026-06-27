using ExamShield.Api.Contracts;
using ExamShield.Application.Queries.GetStudentResults;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/student/results", async (Guid studentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStudentResultsQuery(studentId), ct);
            var items = result.Results
                .Select(r => new StudentResultItemResponse(
                    r.ScoreId, r.CaptureId, r.ExamId, r.ExamName,
                    r.CorrectAnswers, r.TotalQuestions, r.Percentage,
                    r.ScoredAt, r.HashHex, r.IsVerified))
                .ToList();
            return Results.Ok(new StudentResultsResponse(result.StudentId, items));
        })
        .WithName("GetStudentResults")
        .WithTags("Student")
        .RequireAuthorization("Operator")
        .Produces<StudentResultsResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
