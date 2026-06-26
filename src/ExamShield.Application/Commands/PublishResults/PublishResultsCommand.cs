using MediatR;

namespace ExamShield.Application.Commands.PublishResults;

public sealed record PublishResultsCommand(Guid ExamId) : IRequest<PublishResultsResult>;

public sealed record PublishResultsResult(int PublishedCount);
