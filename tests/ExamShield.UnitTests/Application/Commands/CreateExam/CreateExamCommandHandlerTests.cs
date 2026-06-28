using ExamShield.Application.Commands.CreateExam;
using ExamShield.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.CreateExam;

public sealed class CreateExamCommandHandlerTests
{
    private readonly IExamRepository _exams = Substitute.For<IExamRepository>();
    private readonly CreateExamCommandHandler _sut;

    public CreateExamCommandHandlerTests() => _sut = new(_exams);

    private static CreateExamCommand BasicCommand(string name = "Math Final") =>
        new(name, "Description", 40);

    [Fact]
    public async Task Handle_ReturnsResultWithNonEmptyExamId()
    {
        var result = await _sut.Handle(BasicCommand(), default);
        result.ExamId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectName()
    {
        var result = await _sut.Handle(BasicCommand("Physics Final"), default);
        result.Name.Should().Be("Physics Final");
    }

    [Fact]
    public async Task Handle_ReturnsTotalQuestions()
    {
        var result = await _sut.Handle(new("Test", null, 50), default);
        result.TotalQuestions.Should().Be(50);
    }

    [Fact]
    public async Task Handle_PersistsExamViaAddAsync()
    {
        await _sut.Handle(BasicCommand(), default);
        await _exams.Received(1).AddAsync(Arg.Any<ExamShield.Domain.Entities.Exam>(), default);
    }

    [Fact]
    public async Task Handle_DefaultStatusIsDraft()
    {
        var result = await _sut.Handle(BasicCommand(), default);
        result.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Handle_PropagatesScheduledAt()
    {
        var scheduled = DateTimeOffset.UtcNow.AddDays(7);
        var result = await _sut.Handle(new("Exam", null, 10, ScheduledAt: scheduled), default);
        result.ScheduledAt.Should().BeCloseTo(scheduled, TimeSpan.FromSeconds(1));
    }
}
