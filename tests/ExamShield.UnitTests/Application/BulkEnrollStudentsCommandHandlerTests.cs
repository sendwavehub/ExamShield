using ExamShield.Application.Commands.BulkEnrollStudents;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application;

public sealed class BulkEnrollStudentsCommandHandlerTests
{
    private readonly IExamRepository _exams = Substitute.For<IExamRepository>();
    private readonly IExamCandidateRepository _candidates = Substitute.For<IExamCandidateRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private BulkEnrollStudentsCommandHandler CreateHandler() =>
        new(_exams, _candidates, _audit);

    private static Exam CreateExam() => Exam.Create("Mock Exam", null, 10);

    [Fact]
    public async Task Handle_AllNew_EnrollsAll()
    {
        var exam = CreateExam();
        var ids  = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _exams.GetByIdAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>()).Returns(exam);
        _candidates.ExistsAsync(Arg.Any<ExamId>(), Arg.Any<StudentId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new BulkEnrollStudentsCommand(exam.Id.Value, ids), CancellationToken.None);

        Assert.Equal(3, result.Enrolled);
        Assert.Equal(0, result.AlreadyEnrolled);
    }

    [Fact]
    public async Task Handle_SomeExisting_SkipsDuplicates()
    {
        var exam   = CreateExam();
        var newId  = Guid.NewGuid();
        var dupId  = Guid.NewGuid();
        _exams.GetByIdAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>()).Returns(exam);
        _candidates.ExistsAsync(exam.Id, new StudentId(dupId), Arg.Any<CancellationToken>()).Returns(true);
        _candidates.ExistsAsync(exam.Id, new StudentId(newId), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(
            new BulkEnrollStudentsCommand(exam.Id.Value, [newId, dupId]), CancellationToken.None);

        Assert.Equal(1, result.Enrolled);
        Assert.Equal(1, result.AlreadyEnrolled);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsZeroCounts()
    {
        var exam = CreateExam();
        _exams.GetByIdAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>()).Returns(exam);

        var result = await CreateHandler().Handle(
            new BulkEnrollStudentsCommand(exam.Id.Value, []), CancellationToken.None);

        Assert.Equal(0, result.Enrolled);
        Assert.Equal(0, result.AlreadyEnrolled);
    }

    [Fact]
    public async Task Handle_UnknownExam_ThrowsKeyNotFound()
    {
        _exams.GetByIdAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>())
            .Returns((Exam?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateHandler().Handle(
                new BulkEnrollStudentsCommand(Guid.NewGuid(), [Guid.NewGuid()]),
                CancellationToken.None));
    }
}
