using System.Security.Cryptography;
using System.Text;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamShield.Infrastructure.Persistence;

/// <summary>
/// Seeds demo data for every dashboard menu on first startup.
/// Idempotent: skips if any Exam row already exists.
/// All demo users use password: Demo@1234
/// </summary>
public sealed class DataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ExamShieldDbContext>();

        if (await ctx.Exams.AnyAsync(ct))
        {
            logger.LogInformation("Seed data already present — skipping.");
            return;
        }

        logger.LogInformation("Seeding demo data for all dashboard menus…");

        var pwHash = BCrypt.Net.BCrypt.HashPassword("Demo@1234");

        // ── 1. Users ─────────────────────────────────────────────────────────
        var existingEmails = await ctx.Users.Select(u => u.Email.Value).ToListAsync(ct);

        var seedUsers = new (string Email, string DisplayName, UserRole Role)[]
        {
            ("security@examshield.local",     "Sarah Chen",       UserRole.SecurityAdministrator),
            ("sysadmin@examshield.local",      "Tom Nakamura",     UserRole.SystemAdministrator),
            ("exammanager@examshield.local",   "Emma Wilson",      UserRole.ExamManager),
            ("devicemgr@examshield.local",     "David Park",       UserRole.DeviceManager),
            ("invigilator1@examshield.local",  "James Smith",      UserRole.Invigilator),
            ("invigilator2@examshield.local",  "Lisa Johnson",     UserRole.Invigilator),
            ("reviewer@examshield.local",      "Priya Patel",      UserRole.ManualReviewer),
            ("supervisor@examshield.local",    "Robert Lee",       UserRole.ReviewSupervisor),
            ("auditor@examshield.local",       "Angela Foster",    UserRole.Auditor),
            ("publisher@examshield.local",     "Michael Torres",   UserRole.ResultPublisher),
            ("student1@examshield.local",      "Alice Brown",      UserRole.Student),
            ("student2@examshield.local",      "Bob Nguyen",       UserRole.Student),
            ("student3@examshield.local",      "Carol Zhang",      UserRole.Student),
            ("investigator@examshield.local",  "Ivan Petrov",      UserRole.InvestigationOfficer),
        };

        foreach (var (email, displayName, role) in seedUsers)
        {
            if (existingEmails.Contains(email)) continue;
            var u = User.Create(new Email(email), pwHash, role);
            u.UpdateProfile(displayName);
            ctx.Users.Add(u);
        }
        await ctx.SaveChangesAsync(ct);

        // Load all users to memory — EF Core can't translate Email.Value in a Where predicate
        var allUsers   = await ctx.Users.ToListAsync(ct);
        var reviewer   = allUsers.First(u => u.Email.Value == "reviewer@examshield.local");
        var supervisor = allUsers.First(u => u.Email.Value == "supervisor@examshield.local");
        var student1   = allUsers.First(u => u.Email.Value == "student1@examshield.local");
        var student2   = allUsers.First(u => u.Email.Value == "student2@examshield.local");
        var student3   = allUsers.First(u => u.Email.Value == "student3@examshield.local");

        var s1 = new StudentId(student1.Id.Value);
        var s2 = new StudentId(student2.Id.Value);
        var s3 = new StudentId(student3.Id.Value);

        // ── 2. Devices ────────────────────────────────────────────────────────
        var deviceAlpha = MakeDevice("iPad Station Alpha",    0x04, DeviceStatus.Approved);
        var deviceBeta  = MakeDevice("Android Tablet Beta",   0x05, DeviceStatus.Approved);
        var deviceGamma = MakeDevice("iPhone Station Gamma",  0x06, DeviceStatus.Pending);
        var deviceDelta = MakeDevice("Tablet Station Delta",  0x07, DeviceStatus.Disabled);

        ctx.Devices.AddRange(deviceAlpha, deviceBeta, deviceGamma, deviceDelta);
        await ctx.SaveChangesAsync(ct);

        // ── 3. Exams ──────────────────────────────────────────────────────────
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
        chemExam.ClearDomainEvents(); // stays Draft

        ctx.Exams.AddRange(mathExam, physicsExam, chemExam);
        await ctx.SaveChangesAsync(ct);

        // ── 4. Answer Keys ────────────────────────────────────────────────────
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

        // ── 5. Candidates ─────────────────────────────────────────────────────
        var allCandidates = new[]
        {
            ExamCandidate.Enroll(mathExam.Id,    s1),
            ExamCandidate.Enroll(mathExam.Id,    s2),
            ExamCandidate.Enroll(mathExam.Id,    s3),
            ExamCandidate.Enroll(physicsExam.Id, s1),
            ExamCandidate.Enroll(physicsExam.Id, s2),
            ExamCandidate.Enroll(physicsExam.Id, s3),
        };
        foreach (var c in allCandidates) c.ClearDomainEvents();
        ctx.ExamCandidates.AddRange(allCandidates);
        await ctx.SaveChangesAsync(ct);

        // ── 6. Captures ───────────────────────────────────────────────────────
        var sig = new Signature(Enumerable.Repeat((byte)0xFF, 64).ToArray());

        // Math (Closed) — all Verified
        var capM1 = MakeCapture(mathExam.Id, s1, deviceAlpha.Id, "math-s1",  CaptureStatus.Verified, sig);
        var capM2 = MakeCapture(mathExam.Id, s2, deviceAlpha.Id, "math-s2",  CaptureStatus.Verified, sig);
        var capM3 = MakeCapture(mathExam.Id, s3, deviceBeta.Id,  "math-s3",  CaptureStatus.Verified, sig);

        // Physics (Active) — mixed statuses
        var capP1 = MakeCapture(physicsExam.Id, s1, deviceBeta.Id,  "physics-s1", CaptureStatus.Verified,  sig);
        var capP2 = MakeCapture(physicsExam.Id, s2, deviceAlpha.Id, "physics-s2", CaptureStatus.Uploaded,  sig);
        var capP3 = MakeCapture(physicsExam.Id, s3, deviceBeta.Id,  "physics-s3", CaptureStatus.Created,   sig);

        ctx.Captures.AddRange(capM1, capM2, capM3, capP1, capP2, capP3);
        await ctx.SaveChangesAsync(ct);

        // ── 7. OCR Results ────────────────────────────────────────────────────
        var hi = new OcrConfidence(0.96);
        var lo = new OcrConfidence(0.62);

        // Math student 1 — all correct, all high confidence
        var ansM1 = mathRawKey.Select(kv => new ExtractedAnswer(kv.Key, kv.Value, hi)).ToList();

        // Math student 2 — 8/10 correct, high confidence
        var ansM2 = new List<ExtractedAnswer>
        {
            A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),
            A(6,"B",hi),A(7,"C",hi),A(8,"A",hi), // wrong: D→A
            A(9,"A",hi),A(10,"C",hi),             // wrong: B→C
        };

        // Math student 3 — 8/10, some low confidence → manual review
        var ansM3 = new List<ExtractedAnswer>
        {
            A(1,"A",hi),A(2,"B",hi),A(3,"C",hi),A(4,"D",hi),A(5,"A",hi),
            A(6,"B",lo), // low confidence
            A(7,"C",lo), // low confidence
            A(8,"D",hi),A(9,"A",hi),A(10,"B",hi),
        };

        // Physics student 1 — all correct, high confidence
        var ansP1 = physicsRawKey.Select(kv => new ExtractedAnswer(kv.Key, kv.Value, hi)).ToList();

        // Physics student 2 — some low confidence → manual review
        var ansP2 = new List<ExtractedAnswer>
        {
            A(1,"C",lo),A(2,"A",hi),A(3,"D",lo),A(4,"B",hi),A(5,"C",hi),
            A(6,"A",hi),A(7,"D",hi),A(8,"B",hi),A(9,"C",hi),A(10,"A",hi),
        };

        var ocrM1 = OcrResult.Create(capM1.Id, ansM1, 0.85); ocrM1.ClearDomainEvents();
        var ocrM2 = OcrResult.Create(capM2.Id, ansM2, 0.85); ocrM2.ClearDomainEvents();
        var ocrM3 = OcrResult.Create(capM3.Id, ansM3, 0.85); ocrM3.ClearDomainEvents();
        var ocrP1 = OcrResult.Create(capP1.Id, ansP1, 0.85); ocrP1.ClearDomainEvents();
        var ocrP2 = OcrResult.Create(capP2.Id, ansP2, 0.85); ocrP2.ClearDomainEvents();

        ctx.OcrResults.AddRange(ocrM1, ocrM2, ocrM3, ocrP1, ocrP2);
        await ctx.SaveChangesAsync(ct);

        // ── 8. Manual Reviews ─────────────────────────────────────────────────
        // Pending — waiting for reviewer to handle Math s3 low-confidence
        var rev1 = ManualReview.CreateFor(ocrM3);
        rev1.ClearDomainEvents();

        // Completed + Approved — Physics s2 handled and approved
        var rev2 = ManualReview.CreateFor(ocrP2);
        var correctedAnswers = physicsRawKey
            .Select(kv => new ReviewedAnswer(kv.Key, kv.Value)).ToList();
        rev2.Complete(correctedAnswers, reviewer.Id);
        rev2.Approve(supervisor.Id);
        rev2.ClearDomainEvents();

        ctx.ManualReviews.AddRange(rev1, rev2);
        await ctx.SaveChangesAsync(ct);

        // ── 9. Scores (Math — Closed, published) ─────────────────────────────
        var mathKey = mathAnswerKey.ToValueObject();

        var score1 = Score.Create(capM1.Id, mathExam.Id, s1, ansM1, mathKey); score1.Publish(); score1.ClearDomainEvents();
        var score2 = Score.Create(capM2.Id, mathExam.Id, s2, ansM2, mathKey); score2.Publish(); score2.ClearDomainEvents();
        var score3 = Score.Create(capM3.Id, mathExam.Id, s3, ansM3, mathKey); score3.Publish(); score3.ClearDomainEvents();

        ctx.Scores.AddRange(score1, score2, score3);
        await ctx.SaveChangesAsync(ct);

        // ── 10. Audit Logs ────────────────────────────────────────────────────
        var adminId = allUsers.FirstOrDefault(u => u.Email.Value == "admin@examshield.local")?.Id.Value.ToString() ?? "system";
        var logs = new[]
        {
            AuditLog.Record(AuditAction.UserCreated,             null,       "system",          "127.0.0.1",   "Seed: demo users created"),
            AuditLog.Record(AuditAction.DeviceRegistered,        null,       adminId,           "192.168.1.1"),
            AuditLog.Record(AuditAction.AnswerKeySet,            null,       "exammanager@examshield.local", "192.168.1.40"),
            AuditLog.Record(AuditAction.StudentEnrolled,         null,       "exammanager@examshield.local", "192.168.1.40"),
            AuditLog.Record(AuditAction.CaptureRegistered,       capM1.Id,  student1.Id.Value.ToString(), "192.168.1.20"),
            AuditLog.Record(AuditAction.ImageUploaded,           capM1.Id,  student1.Id.Value.ToString(), "192.168.1.20"),
            AuditLog.Record(AuditAction.HashVerified,            capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.OCRCompleted,            capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.ManualReviewStarted,     capM3.Id,  reviewer.Id.Value.ToString(), "192.168.1.30"),
            AuditLog.Record(AuditAction.ManualReviewCompleted,   capP2.Id,  reviewer.Id.Value.ToString(), "192.168.1.30"),
            AuditLog.Record(AuditAction.ReviewApproved,          capP2.Id,  supervisor.Id.Value.ToString(), "192.168.1.31"),
            AuditLog.Record(AuditAction.ScoreGenerated,          capM1.Id,  "system",          "127.0.0.1"),
            AuditLog.Record(AuditAction.ResultPublished,         null,       "publisher@examshield.local", "192.168.1.50"),
            AuditLog.Record(AuditAction.UserRoleChanged,         null,       adminId,           "192.168.1.1",  "Promoted invigilator to reviewer"),
            AuditLog.Record(AuditAction.SettingsUpdated,         null,       adminId,           "192.168.1.1"),
        };
        ctx.AuditLogs.AddRange(logs);
        await ctx.SaveChangesAsync(ct);

        // ── 11. Security Events ───────────────────────────────────────────────
        ctx.SecurityEvents.AddRange(
            Evt(SecurityEventType.LoginSuccess,      SecuritySeverity.Info,     "Successful admin login",                                          adminId,                           "192.168.1.1"),
            Evt(SecurityEventType.LoginSuccess,      SecuritySeverity.Info,     "Successful invigilator login",                                    student1.Id.Value.ToString(),      "192.168.1.20"),
            Evt(SecurityEventType.LoginFailed,       SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",    student1.Id.Value.ToString(),      "10.0.0.5"),
            Evt(SecurityEventType.LoginFailed,       SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",    student1.Id.Value.ToString(),      "10.0.0.5"),
            Evt(SecurityEventType.LoginFailed,       SecuritySeverity.Warning,  "Failed login: student1@examshield.local (incorrect password)",    student1.Id.Value.ToString(),      "10.0.0.5"),
            Evt(SecurityEventType.SuspiciousLogin,   SecuritySeverity.High,     "Account locked after 3 failed attempts from 10.0.0.5",           student1.Id.Value.ToString(),      "10.0.0.5"),
            Evt(SecurityEventType.UnauthorizedAccess,SecuritySeverity.High,     "Access denied: Invigilator role attempted GET /admin/scores",      "invigilator1@examshield.local",  "192.168.1.20"),
            Evt(SecurityEventType.HashMismatch,      SecuritySeverity.Critical, $"SHA-256 mismatch on capture {capP2.Id.Value} — possible tampering", null,                          "192.168.1.25", capP2.Id.Value),
            Evt(SecurityEventType.InvalidSignature,  SecuritySeverity.Critical, "Device signature verification failed — upload rejected",          null,                              "10.0.0.99")
        );
        await ctx.SaveChangesAsync(ct);

        // ── 12. Review Requests ───────────────────────────────────────────────
        var rr1 = ReviewRequest.Submit(s1, capM1.Id,
            "I believe question 8 was marked incorrectly. My written answer was 'D' but OCR read 'A'.");
        rr1.ClearDomainEvents();

        var rr2 = ReviewRequest.Submit(s2, capM2.Id,
            "Questions 9 and 10 appear mis-read — my handwriting may have caused OCR errors.");
        rr2.Resolve("Re-evaluated by supervisor. Original score confirmed — answer sheet image reviewed and OCR output is correct.");
        rr2.ClearDomainEvents();

        ctx.ReviewRequests.AddRange(rr1, rr2);
        await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed complete: {Users} users, 3 exams, 4 devices, 6 captures, 5 OCR results, " +
            "2 reviews, 3 scores, {Logs} audit entries, 9 security events, 2 review requests.",
            seedUsers.Length, logs.Length);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExtractedAnswer A(int q, string text, OcrConfidence conf) =>
        new(q, text, conf);

    private static SecurityEvent Evt(
        SecurityEventType type, SecuritySeverity severity, string msg,
        string? userId = null, string? ip = null, Guid? captureId = null) =>
        SecurityEvent.Create(type, severity, msg, userId, ip, captureId);

    private static Device MakeDevice(string name, byte keyFill, DeviceStatus targetStatus)
    {
        var pk = new PublicKey(Enumerable.Repeat(keyFill, 65).ToArray());
        var d = Device.Register(name, pk);
        d.ClearDomainEvents();
        if (targetStatus == DeviceStatus.Approved || targetStatus == DeviceStatus.Disabled)
            d.Approve();
        if (targetStatus == DeviceStatus.Disabled)
            d.Disable();
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
