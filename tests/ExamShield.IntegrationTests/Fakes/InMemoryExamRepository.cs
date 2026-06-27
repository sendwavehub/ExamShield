using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryExamRepository : IExamRepository
{
    private readonly ConcurrentDictionary<ExamId, Exam> _store = new();

    public Task AddAsync(Exam exam, CancellationToken ct = default)
    {
        _store[exam.Id] = exam;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Exam exam, CancellationToken ct = default)
    {
        _store[exam.Id] = exam;
        return Task.CompletedTask;
    }

    public Task<Exam?> GetByIdAsync(ExamId id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var exam) ? exam : null);

    public Task<IReadOnlyList<Exam>> ListAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Exam>>(_store.Values.ToList());
}
