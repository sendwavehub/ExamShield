using ExamShield.Domain.Enums;

namespace ExamShield.Application.RolePermissions;

public sealed record RoleDefinition(
    UserRole Role,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Permissions);

public static class RolePermissionDefinitions
{
    public static readonly IReadOnlyList<RoleDefinition> All = new List<RoleDefinition>
    {
        new(UserRole.Operator,
            "Operator",
            "Invigilator — captures and uploads answer sheets.",
            ["capture.read", "capture.write", "upload.write", "exams.read", "result.read"]),

        new(UserRole.Supervisor,
            "Supervisor",
            "Review OCR results, trigger scoring, manage exams.",
            ["capture.read", "ocr.read", "ocr.write", "score.read", "score.write",
             "result.read", "review.write", "exams.read", "exams.write"]),

        new(UserRole.Auditor,
            "Auditor",
            "Read-only access to all logs, results, and chain of custody.",
            ["capture.read", "audit.read", "ocr.read", "score.read",
             "result.read", "security.read", "users.read", "devices.read"]),

        new(UserRole.Administrator,
            "Administrator",
            "Full user, device, and exam management. Cannot modify scan evidence.",
            ["users.read", "users.write", "users.manage", "devices.read", "devices.manage",
             "exams.read", "exams.write", "audit.read", "result.read",
             "capture.read", "security.read"]),

        new(UserRole.SecurityOfficer,
            "Security Officer",
            "Investigate security events, verify signatures, review device health.",
            ["audit.read", "security.read", "capture.read", "devices.read", "users.read"]),
    };
}
