using ExamShield.Application.Queries.ExportUsers;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class ExportUsersQueryHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly ExportUsersQueryHandler _sut;

    public ExportUsersQueryHandlerTests() => _sut = new ExportUsersQueryHandler(_repo);

    [Fact]
    public async Task Handle_NoUsers_ReturnsCsvHeaderOnly()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.Handle(new ExportUsersQuery(), default);

        var lines = result.Csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Contains("UserId", lines[0]);
        Assert.Contains("Email", lines[0]);
        Assert.Contains("Role", lines[0]);
        Assert.Contains("IsActive", lines[0]);
    }

    [Fact]
    public async Task Handle_WithUsers_ReturnsOneRowPerUser()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(
            [MakeUser("a@x.com", UserRole.Auditor), MakeUser("b@x.com", UserRole.Operator)]);

        var result = await _sut.Handle(new ExportUsersQuery(), default);

        var lines = result.Csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_FiltersUsers()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(
            [MakeUser("alice@x.com", UserRole.Auditor), MakeUser("bob@x.com", UserRole.Operator)]);

        var result = await _sut.Handle(new ExportUsersQuery(Search: "alice"), default);

        Assert.Contains("alice", result.Csv);
        Assert.DoesNotContain("bob", result.Csv);
    }

    [Fact]
    public async Task Handle_WithRoleFilter_FiltersUsers()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(
            [MakeUser("alice@x.com", UserRole.Auditor), MakeUser("bob@x.com", UserRole.Operator)]);

        var result = await _sut.Handle(new ExportUsersQuery(Role: "Auditor"), default);

        Assert.Contains("alice", result.Csv);
        Assert.DoesNotContain("bob", result.Csv);
    }

    [Fact]
    public async Task Handle_FilenameIsTimestamped()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.Handle(new ExportUsersQuery(), default);

        Assert.StartsWith("users-", result.Filename);
        Assert.EndsWith(".csv", result.Filename);
    }

    private static User MakeUser(string email, UserRole role)
        => User.Create(new Email(email), "hashed", role);
}
