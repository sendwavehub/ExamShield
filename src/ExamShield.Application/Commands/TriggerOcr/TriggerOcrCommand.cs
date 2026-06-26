using MediatR;

namespace ExamShield.Application.Commands.TriggerOcr;

public sealed record TriggerOcrCommand(Guid CaptureId) : IRequest<TriggerOcrResult>;

public sealed record TriggerOcrResult(Guid OcrResultId, Domain.Enums.OcrStatus Status, bool RequiresManualReview);
