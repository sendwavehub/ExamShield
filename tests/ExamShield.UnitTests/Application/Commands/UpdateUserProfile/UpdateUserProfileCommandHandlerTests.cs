using ExamShield.Application.Commands.UpdateUserProfile;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly UpdateUserProfileCommandHandler _sut;

    public UpdateUserProfileCommandHandlerTests() =>
        _sut = new UpdateUserProfileCommandHandler(_users);

    private User MakeUser()
    {
        var user = User.Create(new Email("u@examshield.io"), "$2a$hash", UserRole.Invigilator);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidDisplayName_UpdatesProfile()
    {
        var user = MakeUser();
        await _sut.Handle(new UpdateUserProfileCommand(user.Id.Value, "Alice Smith"), default);
        user.DisplayName.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Handle_WithNullDisplayName_ClearsDisplayName()
    {
        var user = MakeUser();
        user.UpdateProfile("Initial Name");
        await _sut.Handle(new UpdateUserProfileCommand(user.Id.Value, null), default);
        user.DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithWhitespaceDisplayName_ThrowsArgumentException()
    {
        var user = MakeUser();
        var act = () => _sut.Handle(new UpdateUserProfileCommand(user.Id.Value, "   "), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_TrimsWhitespace()
    {
        var user = MakeUser();
        await _sut.Handle(new UpdateUserProfileCommand(user.Id.Value, "  Bob Jones  "), default);
        user.DisplayName.Should().Be("Bob Jones");
    }

    [Fact]
    public async Task Handle_PersistsUser()
    {
        var user = MakeUser();
        await _sut.Handle(new UpdateUserProfileCommand(user.Id.Value, "Carol"), default);
        await _users.Received(1).SaveAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        _users.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
              .Returns((User?)null);

        var act = () => _sut.Handle(new UpdateUserProfileCommand(Guid.NewGuid(), "Name"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
