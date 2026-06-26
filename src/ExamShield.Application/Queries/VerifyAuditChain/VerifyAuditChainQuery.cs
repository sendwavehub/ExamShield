using MediatR;

namespace ExamShield.Application.Queries.VerifyAuditChain;

public sealed record VerifyAuditChainQuery(Guid CaptureId) : IRequest<VerifyAuditChainResult>;

public sealed record VerifyAuditChainResult(bool IsValid, int EntryCount, int? FirstBrokenIndex);
