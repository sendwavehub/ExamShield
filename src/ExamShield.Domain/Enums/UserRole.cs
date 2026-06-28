namespace ExamShield.Domain.Enums;

public enum UserRole
{
    // Original 6 roles (kept for backward compatibility)
    Student            = 0,
    Operator           = 1,   // generic operator — kept for existing tests/data
    Supervisor         = 2,
    Auditor            = 3,
    Administrator      = 4,
    SecurityOfficer    = 5,

    // Spec-defined granular roles (added)
    Invigilator        = 10,  // field staff who capture answer sheets
    OcrEngine          = 11,  // automated OCR system account
    ManualReviewer     = 12,  // reviews low-confidence OCR results
    ReviewSupervisor   = 13,  // approves / rejects reviewer decisions
    ScoringEngine      = 14,  // automated scoring system account
    ResultPublisher    = 15,  // controls when results go public
    ExamManager        = 16,  // manages exams, candidates, schedules
    DeviceManager      = 17,  // registers, approves, disables devices
    InvestigationOfficer = 18, // forensic investigation, read-only
    SuperAdministrator = 19,  // top-level admin (cannot touch answer sheets or scores)
    SecurityAdministrator = 20, // signature / certificate management
    SystemAdministrator = 21, // infrastructure / storage / backup
    PublicVerification = 22,  // anonymous public hash/QR verifier
}
