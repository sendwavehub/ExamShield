using MediatR;

namespace ExamShield.Application.Commands.ScoreCapture;

public sealed record ScoreCaptureCommand(Guid CaptureId) : IRequest<ScoreCaptureResult>;

public sealed record ScoreCaptureResult(Guid ScoreId, int CorrectAnswers, int TotalQuestions, double Percentage);
