using MediatR;

namespace ExamShield.Application.Commands.UploadImage;

public sealed record UploadImageCommand(Guid CaptureId, byte[] ImageBytes) : IRequest<UploadImageResult>;

public sealed record UploadImageResult(string StorageKey);
