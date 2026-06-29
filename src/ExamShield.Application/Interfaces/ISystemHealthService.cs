namespace ExamShield.Application.Interfaces;

public interface ISystemHealthService
{
    Task<IReadOnlyDictionary<string, string>> CheckAsync(CancellationToken ct = default);
}
