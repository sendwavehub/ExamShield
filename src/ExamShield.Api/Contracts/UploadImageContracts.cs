namespace ExamShield.Api.Contracts;

public sealed record UploadImageRequest(Guid CaptureId, byte[] ImageBytes);

public sealed record UploadImageResponse(string StorageKey);
