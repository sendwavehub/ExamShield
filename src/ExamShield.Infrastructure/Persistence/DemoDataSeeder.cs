using System.Security.Cryptography;
using System.Text;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence;

/// <summary>
/// Populates the database with representative demo data covering every dashboard module.
/// All demo users share the password Demo@1234.
/// </summary>
public sealed class DemoDataSeeder(ExamShieldDbContext ctx) : IDemoDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var pwHash = BCrypt.Net.BCrypt.HashPassword("Demo@1234");

        // ── Users ─────────────────────────────────────────────────────────────
        var existingEmails = await ctx.Users.Select(u => u.Email.Value).ToListAsync(ct);

        var seedUsers = new (string Email, string DisplayName, UserRole Role)[]
        {
            ("security@examshield.local",    "Sarah Chen",      UserRole.SecurityAdministrator),
            ("sysadmin@examshield.local",    "Tom Nakamura",    UserRole.SystemAdministrator),
            ("exammanager@examshield.local", "Emma Wilson",     UserRole.ExamManager),
            ("devicemgr@examshield.local",   "David Park",      UserRole.DeviceManager),
            ("invigilator1@examshield.local","James Smith",     UserRole.Invigilator),
            ("invigilator2@examshield.local","Lisa Johnson",    UserRole.Invigilator),
            ("reviewer@examshield.local",    "Priya Patel",     UserRole.ManualReviewer),
            ("supervisor@examshield.local",  "Robert Lee",      UserRole.ReviewSupervisor),
            ("auditor@examshield.local",     "Angela Foster",   UserRole.Auditor),
            ("publisher@examshield.local",   "Michael Torres",  UserRole.ResultPublisher),
            ("student1@examshield.local",    "Alice Brown",     UserRole.Student),
            ("student2@examshield.local",    "Bob Nguyen",      UserRole.Student),
            ("student3@examshield.local",    "Carol Zhang",     UserRole.Student),
            ("student4@examshield.local",    "Daniel Kim",      UserRole.Student),
            ("student5@examshield.local",    "Eva Martinez",    UserRole.Student),
            ("student6@examshield.local",    "Frank Okafor",    UserRole.Student),
            ("student7@examshield.local",    "Grace Liu",       UserRole.Student),
            ("student8@examshield.local",    "Henry Patel",     UserRole.Student),
            ("student9@examshield.local",    "Irene Santos",    UserRole.Student),
            ("student10@examshield.local",   "James Osei",      UserRole.Student),
            ("student11@examshield.local",   "Karen Ahmed",     UserRole.Student),
            ("student12@examshield.local",   "Liam Dubois",     UserRole.Student),
            ("student13@examshield.local",   "Mia Suzuki",      UserRole.Student),
            ("student14@examshield.local",   "Noah Brennan",    UserRole.Student),
            ("student15@examshield.local",   "Olivia Huang",    UserRole.Student),
            ("investigator@examshield.local","Ivan Petrov",     UserRole.InvestigationOfficer),
        };

        foreach (var (email, displayName, role) in seedUsers)
        {
            if (existingEmails.Contains(email)) continue;
            var u = User.Create(new Email(email), pwHash, role);
            u.UpdateProfile(displayName);
            ctx.Users.Add(u);
        }
        await ctx.SaveChangesAsync(ct);

        var allUsers   = await ctx.Users.ToListAsync(ct);
        var reviewer   = allUsers.First(u => u.Email.Value == "reviewer@examshield.local");
        var supervisor = allUsers.First(u => u.Email.Value == "supervisor@examshield.local");
        var student1   = allUsers.First(u => u.Email.Value == "student1@examshield.local");
        var student2   = allUsers.First(u => u.Email.Value == "student2@examshield.local");
        var student3   = allUsers.First(u => u.Email.Value == "student3@examshield.local");
        var student4   = allUsers.First(u => u.Email.Value == "student4@examshield.local");
        var student5   = allUsers.First(u => u.Email.Value == "student5@examshield.local");
        var student6   = allUsers.First(u => u.Email.Value == "student6@examshield.local");
        var student7   = allUsers.First(u => u.Email.Value == "student7@examshield.local");
        var student8   = allUsers.First(u => u.Email.Value == "student8@examshield.local");
        var student9   = allUsers.First(u => u.Email.Value == "student9@examshield.local");
        var student10  = allUsers.First(u => u.Email.Value == "student10@examshield.local");
        var student11  = allUsers.First(u => u.Email.Value == "student11@examshield.local");
        var student12  = allUsers.First(u => u.Email.Value == "student12@examshield.local");
        var student13  = allUsers.First(u => u.Email.Value == "student13@examshield.local");
        var student14  = allUsers.First(u => u.Email.Value == "student14@examshield.local");
        var student15  = allUsers.First(u => u.Email.Value == "student15@examshield.local");
        var adminUser  = allUsers.FirstOrDefault(u => u.Role == UserRole.SuperAdministrator);
        var adminId    = adminUser?.Id.Value.ToString() ?? "system";

        var s1  = new StudentId(student1.Id.Value);
        var s2  = new StudentId(student2.Id.Value);
        var s3  = new StudentId(student3.Id.Value);
        var s4  = new StudentId(student4.Id.Value);
        var s5  = new StudentId(student5.Id.Value);
        var s6  = new StudentId(student6.Id.Value);
        var s7  = new StudentId(student7.Id.Value);
        var s8  = new StudentId(student8.Id.Value);
        var s9  = new StudentId(student9.Id.Value);
        var s10 = new StudentId(student10.Id.Value);
        var s11 = new StudentId(student11.Id.Value);
        var s12 = new StudentId(student12.Id.Value);
        var s13 = new StudentId(student13.Id.Value);
        var s14 = new StudentId(student14.Id.Value);
        var s15 = new StudentId(student15.Id.Value);

        // ── Devices ───────────────────────────────────────────────────────────
        var deviceAlpha = MakeDevice("iPad Station Alpha",   0x04, DeviceStatus.Approved);
        var deviceBeta  = MakeDevice("Android Tablet Beta",  0x05, DeviceStatus.Approved);
        var deviceGamma = MakeDevice("iPhone Station Gamma", 0x06, DeviceStatus.Pending);
        var deviceDelta = MakeDevice("Tablet Station Delta", 0x07, DeviceStatus.Disabled);
        ctx.Devices.AddRange(deviceAlpha, deviceBeta, deviceGamma, deviceDelta);
        await ctx.SaveChangesAsync(ct);

        // ── Exams ─────────────────────────────────────────────────────────────
        var now = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

        var mathExam = Exam.Create("Mathematics Final 2026",
            "Comprehensive final covering algebra, calculus, and statistics.",
            10, scheduledAt: now.AddMonths(-1), endsAt: now.AddMonths(-1).AddHours(3), maxCandidates: 100);
        mathExam.Activate(); mathExam.Close(); mathExam.ClearDomainEvents();

        var physicsExam = Exam.Create("Physics Midterm 2026",
            "Mid-semester assessment on mechanics, thermodynamics, and waves.",
            10, scheduledAt: now.AddDays(2), endsAt: now.AddDays(2).AddHours(2), maxCandidates: 80);
        physicsExam.Activate(); physicsExam.ClearDomainEvents();

        var chemExam = Exam.Create("Chemistry Quiz I",
            "First quiz covering periodic table, atomic structure, and bonding.",
            5, scheduledAt: now.AddDays(14), endsAt: now.AddDays(14).AddHours(1), maxCandidates: 60);
        chemExam.ClearDomainEvents();

        ctx.Exams.AddRange(mathExam, physicsExam, chemExam);
        await ctx.SaveChangesAsync(ct);

        // ── Answer Keys ───────────────────────────────────────────────────────
        var mathRawKey = new Dictionary<int, string>
        {
            [1]="A",[2]="B",[3]="C",[4]="D",[5]="A",
            [6]="B",[7]="C",[8]="D",[9]="A",[10]="B"
        };
        var physicsRawKey = new Dictionary<int, string>
        {
            [1]="C",[2]="A",[3]="D",[4]="B",[5]="C",
            [6]="A",[7]="D",[8]="B",[9]="C",[10]="A"
        };

        var mathAnswerKey    = ExamAnswerKey.Create(mathExam.Id,    mathRawKey);
        var physicsAnswerKey = ExamAnswerKey.Create(physicsExam.Id, physicsRawKey);
        mathAnswerKey.ClearDomainEvents();
        physicsAnswerKey.ClearDomainEvents();
        ctx.ExamAnswerKeys.AddRange(mathAnswerKey, physicsAnswerKey);
        await ctx.SaveChangesAsync(ct);

        // ── Candidates ────────────────────────────────────────────────────────
        var candidates = new[]
        {
            ExamCandidate.Enroll(mathExam.Id,    s1),  ExamCandidate.Enroll(mathExam.Id,    s2),
            ExamCandidate.Enroll(mathExam.Id,    s3),  ExamCandidate.Enroll(physicsExam.Id, s1),
            ExamCandidate.Enroll(physicsExam.Id, s2),  ExamCandidate.Enroll(physicsExam.Id, s3),
            ExamCandidate.Enroll(mathExam.Id, s4),  ExamCandidate.Enroll(mathExam.Id, s5),
            ExamCandidate.Enroll(mathExam.Id, s6),  ExamCandidate.Enroll(mathExam.Id, s7),
            ExamCandidate.Enroll(mathExam.Id, s8),  ExamCandidate.Enroll(mathExam.Id, s9),
            ExamCandidate.Enroll(mathExam.Id, s10), ExamCandidate.Enroll(mathExam.Id, s11),
            ExamCandidate.Enroll(mathExam.Id, s12), ExamCandidate.Enroll(mathExam.Id, s13),
            ExamCandidate.Enroll(mathExam.Id, s14), ExamCandidate.Enroll(mathExam.Id, s15),
        };
        foreach (var c in candidates) c.ClearDomainEvents();
        ctx.ExamCandidates.AddRange(candidates);
        await ctx.SaveChangesAsync(ct);

        // ── Captures ──────────────────────────────────────────────────────────
        var sig = new Signature(Enumerable.Repeat((byte)0xFF, 64).ToArray());

        var capM1  = MakeCapture(mathExam.Id,    s1,  deviceAlpha.Id, "math-s1",   CaptureStatus.Verified, sig);
        var capM2  = MakeCapture(mathExam.Id,    s2,  deviceAlpha.Id, "math-s2",   CaptureStatus.Verified, sig);
        var capM3  = MakeCapture(mathExam.Id,    s3,  deviceBeta.Id,  "math-s3",   CaptureStatus.Verified, sig);
        var capP1  = MakeCapture(physicsExam.Id, s1,  deviceBeta.Id,  "physics-s1",CaptureStatus.Verified, sig);
        var capP2  = MakeCapture(physicsExam.Id, s2,  deviceAlpha.Id, "physics-s2",CaptureStatus.Uploaded, sig);
        var capP3  = MakeCapture(physicsExam.Id, s3,  deviceBeta.Id,  "physics-s3",CaptureStatus.Created,  sig);
        var capM4  = MakeCapture(mathExam.Id,    s4,  deviceAlpha.Id, "math-s4",   CaptureStatus.Verified, sig);
        var capM5  = MakeCapture(mathExam.Id,    s5,  deviceBeta.Id,  "math-s5",   CaptureStatus.Verified, sig);
        var capM6  = MakeCapture(mathExam.Id,    s6,  deviceAlpha.Id, "math-s6",   CaptureStatus.Verified, sig);
        var capM7  = MakeCapture(mathExam.Id,    s7,  deviceBeta.Id,  "math-s7",   CaptureStatus.Verified, sig);
        var capM8  = MakeCapture(mathExam.Id,    s8,  deviceAlpha.Id, "math-s8",   CaptureStatus.Verified, sig);
        var capM9  = MakeCapture(mathExam.Id,    s9,  deviceBeta.Id,  "math-s9",   CaptureStatus.Verified, sig);
        var capM10 = MakeCapture(mathExam.Id,    s10, deviceAlpha.Id, "math-s10",  CaptureStatus.Verified, sig);
        var capM11 = MakeCapture(mathExam.Id,    s11, deviceBeta.Id,  "math-s11",  CaptureStatus.Verified, sig);
        var capM12 = MakeCapture(mathExam.Id,    s12, deviceAlpha.Id, "math-s12",  CaptureStatus.Verified, sig);
        var capM13 = MakeCapture(mathExam.Id,    s13, deviceBeta.Id,  "math-s13",  CaptureStatus.Verified, sig);
        var capM14 = MakeCapture(mathExam.Id,    s14, deviceAlpha.Id, "math-s14",  CaptureStatus.Verified, sig);
        var capM15 = MakeCapture(mathExam.Id,    s15, deviceBeta.Id,  "math-s15",  CaptureStatus.Verified, sig);

        ctx.Captures.AddRange(capM1, capM2, capM3, capP1, capP2, capP3,
                              capM4, capM5, capM6, capM7, capM8, capM9,
                              capM10, capM11, capM12, capM13, capM14, capM15);
        await ctx.SaveChangesAsync(ct);

        // ── OCR Results ───────────────────────────────────────────────────────
        var hi = new OcrConfidence(0.96);
        var lo = new OcrConfidence(0.62);

        var ansM1 = mathRawKey.Select(kv => new ExtractedAnswer(kv.Key, kv.Value, hi)).ToList();
        var ansM2 = new List<ExtractedAnswer>
        {
            A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),
            A(6,"B",hi),A(7,"C",hi),A(8,"A",hi),A(9,"A",hi),A(10,"C",hi),
        };
        var ansM3 = new List<ExtractedAnswer>
        {
            A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),
            A(6,"B",lo),A(7,"C",lo),A(8,"D",hi),A(9,"A",hi),A(10,"B",hi),
        };
        var ansP1 = physicsRawKey.Select(kv => new ExtractedAnswer(kv.Key, kv.Value, hi)).ToList();
        var ansP2 = new List<ExtractedAnswer>
        {
            A(1,"C",lo),A(2,"A",hi),A(3,"D",lo),A(4,"B",hi),A(5,"C",hi),
            A(6,"A",hi),A(7,"D",hi),A(8,"B",hi),A(9,"C",hi),A(10,"A",hi),
        };

        var ans100 = mathRawKey.Select(kv => A(kv.Key, kv.Value, hi)).ToList();
        var ans90  = new List<ExtractedAnswer> { A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),A(6,"B",hi),A(7,"C",hi),A(8,"D",hi),A(9,"A",hi),A(10,"C",hi) };
        var ans80  = new List<ExtractedAnswer> { A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),A(6,"B",hi),A(7,"C",hi),A(8,"C",hi),A(9,"A",hi),A(10,"C",hi) };
        var ans70  = new List<ExtractedAnswer> { A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),A(6,"A",hi),A(7,"C",hi),A(8,"C",hi),A(9,"A",hi),A(10,"C",hi) };
        var ans60  = new List<ExtractedAnswer> { A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"C",hi),A(5,"A",hi),A(6,"A",hi),A(7,"C",hi),A(8,"C",hi),A(9,"A",hi),A(10,"C",hi) };
        var ans50  = new List<ExtractedAnswer> { A(1,"A",hi),A(2,"A",hi),A(3,"C",hi),A(4,"C",hi),A(5,"A",hi),A(6,"A",hi),A(7,"C",hi),A(8,"C",hi),A(9,"A",hi),A(10,"C",hi) };

        var ocrResults = new[]
        {
            Create(capM1.Id, ansM1), Create(capM2.Id, ansM2), Create(capM3.Id, ansM3),
            Create(capP1.Id, ansP1), Create(capP2.Id, ansP2),
            Create(capM4.Id, ans100), Create(capM5.Id, ans100),
            Create(capM6.Id, ans90),  Create(capM7.Id, ans90),
            Create(capM8.Id, ans80),  Create(capM9.Id, ans80),  Create(capM10.Id, ans80),
            Create(capM11.Id, ans70), Create(capM12.Id, ans70),
            Create(capM13.Id, ans60), Create(capM14.Id, ans60),
            Create(capM15.Id, ans50),
        };
        ctx.OcrResults.AddRange(ocrResults);
        await ctx.SaveChangesAsync(ct);

        // ── Manual Reviews ────────────────────────────────────────────────────
        var ocrM3 = ocrResults[2];
        var ocrP2 = ocrResults[4];

        var rev1 = ManualReview.CreateFor(ocrM3); rev1.ClearDomainEvents();
        var rev2 = ManualReview.CreateFor(ocrP2);
        rev2.Complete(physicsRawKey.Select(kv => new ReviewedAnswer(kv.Key, kv.Value)).ToList(), reviewer.Id);
        rev2.Approve(supervisor.Id); rev2.ClearDomainEvents();
        ctx.ManualReviews.AddRange(rev1, rev2);
        await ctx.SaveChangesAsync(ct);

        // ── Scores ────────────────────────────────────────────────────────────
        var mathKey    = mathAnswerKey.ToValueObject();
        var physicsKey = physicsAnswerKey.ToValueObject();

        static Score Scored(CaptureId capId, ExamId examId, StudentId sId,
            List<ExtractedAnswer> ans, AnswerKey key)
        {
            var s = Score.Create(capId, examId, sId, ans, key);
            s.Publish(); s.ClearDomainEvents(); return s;
        }

        var ansPS2 = new List<ExtractedAnswer> { A(1,"C",hi),A(2,"A",hi),A(3,"D",hi),A(4,"B",hi),A(5,"C",hi),A(6,"A",hi),A(7,"A",hi),A(8,"A",hi),A(9,"C",hi),A(10,"C",hi) };
        var ansPS3 = new List<ExtractedAnswer> { A(1,"C",hi),A(2,"A",hi),A(3,"A",hi),A(4,"B",hi),A(5,"C",hi),A(6,"A",hi),A(7,"A",hi),A(8,"A",hi),A(9,"C",hi),A(10,"C",hi) };

        ctx.Scores.AddRange(
            Scored(capM1.Id,  mathExam.Id,    s1,  ansM1,  mathKey),
            Scored(capM2.Id,  mathExam.Id,    s2,  ansM2,  mathKey),
            Scored(capM3.Id,  mathExam.Id,    s3,  ansM3,  mathKey),
            Scored(capM4.Id,  mathExam.Id,    s4,  ans100, mathKey),
            Scored(capM5.Id,  mathExam.Id,    s5,  ans100, mathKey),
            Scored(capM6.Id,  mathExam.Id,    s6,  ans90,  mathKey),
            Scored(capM7.Id,  mathExam.Id,    s7,  ans90,  mathKey),
            Scored(capM8.Id,  mathExam.Id,    s8,  ans80,  mathKey),
            Scored(capM9.Id,  mathExam.Id,    s9,  ans80,  mathKey),
            Scored(capM10.Id, mathExam.Id,    s10, ans80,  mathKey),
            Scored(capM11.Id, mathExam.Id,    s11, ans70,  mathKey),
            Scored(capM12.Id, mathExam.Id,    s12, ans70,  mathKey),
            Scored(capM13.Id, mathExam.Id,    s13, ans60,  mathKey),
            Scored(capM14.Id, mathExam.Id,    s14, ans60,  mathKey),
            Scored(capM15.Id, mathExam.Id,    s15, ans50,  mathKey),
            Scored(capP1.Id,  physicsExam.Id, s1,  ansP1,  physicsKey),
            Scored(capP2.Id,  physicsExam.Id, s2,  ansPS2, physicsKey),
            Scored(capP3.Id,  physicsExam.Id, s3,  ansPS3, physicsKey)
        );
        await ctx.SaveChangesAsync(ct);

        // ── Audit Logs ────────────────────────────────────────────────────────
        ctx.AuditLogs.AddRange(
            AuditLog.Record(AuditAction.UserCreated,           null,       "system",         "127.0.0.1",  "Seed: demo users created"),
            AuditLog.Record(AuditAction.DeviceRegistered,      null,       adminId,          "192.168.1.1"),
            AuditLog.Record(AuditAction.AnswerKeySet,          null,       "exammanager@examshield.local", "192.168.1.40"),
            AuditLog.Record(AuditAction.StudentEnrolled,       null,       "exammanager@examshield.local", "192.168.1.40"),
            AuditLog.Record(AuditAction.CaptureRegistered,     capM1.Id,  student1.Id.Value.ToString(), "192.168.1.20"),
            AuditLog.Record(AuditAction.ImageUploaded,         capM1.Id,  student1.Id.Value.ToString(), "192.168.1.20"),
            AuditLog.Record(AuditAction.HashVerified,          capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.OCRCompleted,          capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.ManualReviewStarted,   capM3.Id,  reviewer.Id.Value.ToString(), "192.168.1.30"),
            AuditLog.Record(AuditAction.ManualReviewCompleted, capP2.Id,  reviewer.Id.Value.ToString(), "192.168.1.30"),
            AuditLog.Record(AuditAction.ReviewApproved,        capP2.Id,  supervisor.Id.Value.ToString(),"192.168.1.31"),
            AuditLog.Record(AuditAction.ScoreGenerated,        capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.ResultPublished,       null,       "publisher@examshield.local", "192.168.1.50"),
            AuditLog.Record(AuditAction.UserRoleChanged,       null,       adminId,           "192.168.1.1", "Promoted invigilator to reviewer"),
            AuditLog.Record(AuditAction.SettingsUpdated,       null,       adminId,           "192.168.1.1")
        );
        await ctx.SaveChangesAsync(ct);

        // ── Security Events ───────────────────────────────────────────────────
        ctx.SecurityEvents.AddRange(
            Evt(SecurityEventType.LoginSuccess,       SecuritySeverity.Info,     "Successful admin login",                                       adminId,                         "192.168.1.1"),
            Evt(SecurityEventType.LoginSuccess,       SecuritySeverity.Info,     "Successful invigilator login",                                  student1.Id.Value.ToString(),    "192.168.1.20"),
            Evt(SecurityEventType.LoginFailed,        SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",  student1.Id.Value.ToString(),    "10.0.0.5"),
            Evt(SecurityEventType.LoginFailed,        SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",  student1.Id.Value.ToString(),    "10.0.0.5"),
            Evt(SecurityEventType.LoginFailed,        SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",  student1.Id.Value.ToString(),    "10.0.0.5"),
            Evt(SecurityEventType.SuspiciousLogin,    SecuritySeverity.High,     "Account locked after 3 failed attempts from 10.0.0.5",         student1.Id.Value.ToString(),    "10.0.0.5"),
            Evt(SecurityEventType.UnauthorizedAccess, SecuritySeverity.High,     "Access denied: Invigilator attempted GET /admin/scores",        "invigilator1@examshield.local", "192.168.1.20"),
            Evt(SecurityEventType.HashMismatch,       SecuritySeverity.Critical, $"SHA-256 mismatch on capture {capP2.Id.Value} — possible tampering", null,                    "192.168.1.25", capP2.Id.Value),
            Evt(SecurityEventType.InvalidSignature,   SecuritySeverity.Critical, "Device signature verification failed — upload rejected",        null,                            "10.0.0.99")
        );
        await ctx.SaveChangesAsync(ct);

        // ── Review Requests ───────────────────────────────────────────────────
        var rr1 = ReviewRequest.Submit(s1, capM1.Id,
            "I believe question 8 was marked incorrectly. My written answer was 'D' but OCR read 'A'.");
        rr1.ClearDomainEvents();

        var rr2 = ReviewRequest.Submit(s2, capM2.Id,
            "Questions 9 and 10 appear mis-read — my handwriting may have caused OCR errors.");
        rr2.Resolve("Re-evaluated by supervisor. Original score confirmed.");
        rr2.ClearDomainEvents();

        ctx.ReviewRequests.AddRange(rr1, rr2);
        await ctx.SaveChangesAsync(ct);
    }

    private static ExtractedAnswer A(int q, string text, OcrConfidence conf) => new(q, text, conf);

    private static OcrResult Create(CaptureId captureId, List<ExtractedAnswer> answers)
    {
        var r = OcrResult.Create(captureId, answers, 0.85);
        r.ClearDomainEvents();
        return r;
    }

    private static SecurityEvent Evt(
        SecurityEventType type, SecuritySeverity severity, string msg,
        string? userId = null, string? ip = null, Guid? captureId = null) =>
        SecurityEvent.Create(type, severity, msg, userId, ip, captureId);

    private static Device MakeDevice(string name, byte keyFill, DeviceStatus targetStatus)
    {
        var pk = new PublicKey(Enumerable.Repeat(keyFill, 65).ToArray());
        var d = Device.Register(name, pk);
        d.ClearDomainEvents();
        if (targetStatus is DeviceStatus.Approved or DeviceStatus.Disabled) d.Approve();
        if (targetStatus == DeviceStatus.Disabled) d.Disable();
        return d;
    }

    private static Capture MakeCapture(
        ExamId examId, StudentId studentId, DeviceId deviceId,
        string seed, CaptureStatus targetStatus, Signature sig)
    {
        var hash = Hash.FromBytes(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
        var cap = Capture.Create(examId, studentId, deviceId, new PageNumber(1), hash, sig);
        cap.ClearDomainEvents();
        if (targetStatus is CaptureStatus.Uploaded or CaptureStatus.Verified)
            cap.RecordUpload($"captures/{seed}/page1.jpg");
        if (targetStatus == CaptureStatus.Verified)
            cap.VerifyIntegrity(hash);
        cap.ClearDomainEvents();
        return cap;
    }
}
