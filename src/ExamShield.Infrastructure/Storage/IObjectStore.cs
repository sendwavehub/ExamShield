namespace ExamShield.Infrastructure.Storage;

public interface IObjectStore
{
    Task PutAsync(string key, byte[] data, CancellationToken ct);
    Task<byte[]> GetAsync(string key, CancellationToken ct);
}
