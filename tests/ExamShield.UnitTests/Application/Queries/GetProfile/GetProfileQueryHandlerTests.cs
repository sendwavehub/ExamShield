using ExamShield.Application.Queries.GetProfile;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries.GetProfile;

public sealed class GetProfileQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly GetProfileQueryHandler _sut;

    public GetProfileQueryHandlerTests() => _sut = new GetProfileQueryHandler(_users);

    [Fact]
    public async Task Handle_ReturnsUserEmailAndRole()
    {
        var user = User.Create(new Email("op@examshield.io"), "hash", UserRole.Operator);
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new GetProfileQuery(user.Id.Value), default);

        result.Email.Should().Be("op@examshield.io");
        result.Role.Should().Be("Operator");
    }

    [Fact]
    public async Task Handle_ReturnsMfaEnabledStatus()
    {
        var user = User.Create(new Email("a@b.com"), "hash", UserRole.Operator);
        user.SetMfaSecret("SECRET");
        user.EnableMfa();
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new GetProfileQuery(user.Id.Value), default);

        result.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _sut.Handle(new GetProfileQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
