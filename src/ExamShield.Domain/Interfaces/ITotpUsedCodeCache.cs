namespace ExamShield.Domain.Interfaces;

public interface ITotpUsedCodeCache
{
    Task<bool> IsUsedAsync(string userId, string code, CancellationToken ct = default);
    Task MarkUsedAsync(string userId, string code, CancellationToken ct = default);
}
