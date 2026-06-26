using MediatR;

namespace ExamShield.Application.Commands.SubmitReview;

public sealed record ReviewedAnswerDto(int QuestionNumber, string Text);

public sealed record SubmitReviewCommand(
    Guid ReviewId,
    IReadOnlyList<ReviewedAnswerDto> Answers,
    Guid ReviewedByUserId) : IRequest;
