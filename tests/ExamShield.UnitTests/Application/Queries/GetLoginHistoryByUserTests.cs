using ExamShield.Application.Queries.GetLoginHistory;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class GetLoginHistoryByUserTests
{
    private readonly ISecurityEventRepository _repo = Substitute.For<ISecurityEventRepository>();
    private readonly GetLoginHistoryQueryHandler _sut;

    public GetLoginHistoryByUserTests() => _sut = new GetLoginHistoryQueryHandler(_repo);

    [Fact]
    public async Task Handle_WithUserId_PassesUserIdToRepository()
    {
        _repo.ListByTypesAsync(
            Arg.Any<IEnumerable<SecurityEventType>>(), 100,
            null, null, "user-42", default)
            .Returns(new List<SecurityEvent>());

        await _sut.Handle(new GetLoginHistoryQuery(UserId: "user-42"), default);

        await _repo.Received(1).ListByTypesAsync(
            Arg.Any<IEnumerable<SecurityEventType>>(), 100,
            null, null, "user-42", default);
    }

    [Fact]
    public async Task Handle_WithoutUserId_PassesNullUserId()
    {
        _repo.ListByTypesAsync(
            Arg.Any<IEnumerable<SecurityEventType>>(), 100,
            null, null, null, default)
            .Returns(new List<SecurityEvent>());

        await _sut.Handle(new GetLoginHistoryQuery(), default);

        await _repo.Received(1).ListByTypesAsync(
            Arg.Any<IEnumerable<SecurityEventType>>(), 100,
            null, null, null, default);
    }

    [Fact]
    public async Task Handle_WithUserId_ReturnsMappedDtos()
    {
        var evt = SecurityEvent.Create(SecurityEventType.LoginSuccess, SecuritySeverity.Info,
            "login ok", userId: "user-42");
        _repo.ListByTypesAsync(
            Arg.Any<IEnumerable<SecurityEventType>>(), Arg.Any<int>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            "user-42", default)
            .Returns(new List<SecurityEvent> { evt });

        var result = await _sut.Handle(new GetLoginHistoryQuery(UserId: "user-42"), default);

        var dto = Assert.Single(result.Events);
        Assert.Equal("user-42", dto.UserId);
    }
}
