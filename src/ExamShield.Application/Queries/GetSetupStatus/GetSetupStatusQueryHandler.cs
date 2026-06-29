using ExamShield.Application.Interfaces;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetSetupStatus;

public sealed class GetSetupStatusQueryHandler(
    IUserRepository users,
    ISystemHealthService health)
    : IRequestHandler<GetSetupStatusQuery, SetupStatusResult>
{
    public async Task<SetupStatusResult> Handle(GetSetupStatusQuery _, CancellationToken ct)
    {
        var all = await users.ListAllAsync(ct);
        var isFirstRun = !all.Any(u => u.Role == UserRole.SuperAdministrator);
        var checks = await health.CheckAsync(ct);
        return new SetupStatusResult(isFirstRun, "1.0.0", checks);
    }
}
