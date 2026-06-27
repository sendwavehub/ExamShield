using MediatR;

namespace ExamShield.Application.Queries.GetCaptures;

public sealed record CaptureDto(
    Guid CaptureId, Guid ExamId, Guid StudentId, Guid DeviceId,
    string Status, DateTimeOffset CapturedAt, string? StorageKey);

public sealed record GetCapturesResult(IReadOnlyList<CaptureDto> Captures);

public sealed record GetCapturesQuery : IRequest<GetCapturesResult>;
