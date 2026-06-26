using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Storage;

public sealed class MinioImageStorage(IObjectStore objectStore) : IImageStorage
{
    public async Task<string> StoreAsync(Guid captureId, byte[] imageBytes, CancellationToken ct = default)
    {
        var key = $"captures/{captureId:N}";
        await objectStore.PutAsync(key, imageBytes, ct);
        return key;
    }

    public async Task<byte[]> RetrieveAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            return await objectStore.GetAsync(storageKey, ct);
        }
        catch (KeyNotFoundException)
        {
            throw new ImageNotFoundException(storageKey);
        }
    }
}
