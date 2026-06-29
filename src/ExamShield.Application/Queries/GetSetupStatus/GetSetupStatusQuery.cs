using MediatR;

namespace ExamShield.Application.Queries.GetSetupStatus;

public sealed record GetSetupStatusQuery : IRequest<SetupStatusResult>;

public sealed record SetupStatusResult(
    bool IsFirstRun,
    string Version,
    IReadOnlyDictionary<string, string> Checks);
