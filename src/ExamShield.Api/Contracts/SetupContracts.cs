namespace ExamShield.Api.Contracts;

public sealed record SetupStatusResponse(
    bool IsFirstRun,
    string Version,
    IReadOnlyDictionary<string, string> Checks);

public sealed record CompleteSetupRequest(
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword,
    bool SeedDemoData);

public sealed record CompleteSetupResponse(Guid AdminUserId, string Message);
