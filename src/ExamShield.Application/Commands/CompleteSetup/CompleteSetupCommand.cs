using MediatR;

namespace ExamShield.Application.Commands.CompleteSetup;

public sealed record CompleteSetupCommand(
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword,
    bool SeedDemoData
) : IRequest<CompleteSetupResult>;

public sealed record CompleteSetupResult(Guid AdminUserId);
