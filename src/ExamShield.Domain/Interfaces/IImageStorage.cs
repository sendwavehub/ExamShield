namespace ExamShield.Domain.Interfaces;

public interface IImageStorage
{
    Task<string> StoreAsync(Guid captureId, byte[] imageBytes, CancellationToken ct = default);
    Task<byte[]> RetrieveAsync(string storageKey, CancellationToken ct = default);
}
