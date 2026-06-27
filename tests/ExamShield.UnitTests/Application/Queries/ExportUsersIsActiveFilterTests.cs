using ExamShield.Application.Queries.ExportUsers;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class ExportUsersIsActiveFilterTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ExportUsersQueryHandler _sut;

    private static User Active(string email)
    {
        var u = User.Create(new Email(email), "hash", UserRole.Auditor);
        return u;
    }

    private static User Inactive(string email)
    {
        var u = User.Create(new Email(email), "hash", UserRole.Auditor);
        u.Deactivate();
        return u;
    }

    public ExportUsersIsActiveFilterTests()
    {
        var all = new List<User>
        {
            Active("alice@x.com"),
            Inactive("bob@x.com"),
            Active("carol@x.com"),
        };
        _users.ListAllAsync(Arg.Any<CancellationToken>())
              .Returns(all.AsReadOnly());
        _sut = new ExportUsersQueryHandler(_users);
    }

    [Fact]
    public async Task Handle_NoIsActiveFilter_ExportsAllUsers()
    {
        var result = await _sut.Handle(new ExportUsersQuery(), default);
        var lines = CsvDataLines(result.Csv);
        lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_IsActiveTrue_ExportsOnlyActiveUsers()
    {
        var result = await _sut.Handle(new ExportUsersQuery(IsActive: true), default);
        var lines = CsvDataLines(result.Csv);
        lines.Should().HaveCount(2);
        lines.Should().AllSatisfy(l => l.Should().Contain(",True,"));
    }

    [Fact]
    public async Task Handle_IsActiveFalse_ExportsOnlyInactiveUsers()
    {
        var result = await _sut.Handle(new ExportUsersQuery(IsActive: false), default);
        var lines = CsvDataLines(result.Csv);
        lines.Should().HaveCount(1);
        lines.Should().AllSatisfy(l => l.Should().Contain(",False,"));
    }

    [Fact]
    public async Task Handle_IsActiveFalse_EmptyResult_CsvContainsOnlyHeader()
    {
        _users.ListAllAsync(Arg.Any<CancellationToken>())
              .Returns(new List<User> { Active("only@x.com") }.AsReadOnly());

        var result = await _sut.Handle(new ExportUsersQuery(IsActive: false), default);
        var lines = CsvDataLines(result.Csv);
        lines.Should().BeEmpty();
    }

    private static List<string> CsvDataLines(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
           .Skip(1).ToList();
}
