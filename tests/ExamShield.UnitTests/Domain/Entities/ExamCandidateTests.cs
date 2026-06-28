using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class ExamCandidateTests
{
    [Fact]
    public void Enroll_SetsExamAndStudentIds()
    {
        var examId = new ExamId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());

        var candidate = ExamCandidate.Enroll(examId, studentId);

        candidate.ExamId.Should().Be(examId);
        candidate.StudentId.Should().Be(studentId);
    }

    [Fact]
    public void Enroll_SetsEnrolledAtToNow()
    {
        var candidate = ExamCandidate.Enroll(new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()));
        candidate.EnrolledAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Enroll_NullExamId_Throws()
    {
        var act = () => ExamCandidate.Enroll(null!, new StudentId(Guid.NewGuid()));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enroll_NullStudentId_Throws()
    {
        var act = () => ExamCandidate.Enroll(new ExamId(Guid.NewGuid()), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enroll_DifferentStudents_HaveDifferentStudentIds()
    {
        var examId = new ExamId(Guid.NewGuid());
        var s1 = new StudentId(Guid.NewGuid());
        var s2 = new StudentId(Guid.NewGuid());
        var a = ExamCandidate.Enroll(examId, s1);
        var b = ExamCandidate.Enroll(examId, s2);
        a.StudentId.Should().NotBe(b.StudentId);
    }
}
