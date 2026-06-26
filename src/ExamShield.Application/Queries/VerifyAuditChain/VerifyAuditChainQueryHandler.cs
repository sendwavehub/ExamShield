using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.VerifyAuditChain;

public sealed class VerifyAuditChainQueryHandler
    : IRequestHandler<VerifyAuditChainQuery, VerifyAuditChainResult>
{
    private readonly IAuditLogRepository _repository;

    public VerifyAuditChainQueryHandler(IAuditLogRepository repository) =>
        _repository = repository;

    public async Task<VerifyAuditChainResult> Handle(
        VerifyAuditChainQuery query, CancellationToken ct)
    {
        var entries = await _repository.GetChainAsync(new CaptureId(query.CaptureId), ct);

        if (entries.Count == 0)
            return new VerifyAuditChainResult(IsValid: true, EntryCount: 0, FirstBrokenIndex: null);

        var expectedPrevious = string.Empty;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var expectedHash = AuditChainHasher.ComputeContentHash(entry, expectedPrevious);

            if (entry.PreviousHash != expectedPrevious || entry.ContentHash != expectedHash)
                return new VerifyAuditChainResult(IsValid: false, EntryCount: entries.Count, FirstBrokenIndex: i);

            expectedPrevious = entry.ContentHash;
        }

        return new VerifyAuditChainResult(IsValid: true, EntryCount: entries.Count, FirstBrokenIndex: null);
    }
}
