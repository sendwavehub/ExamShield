using ExamShield.Domain.Entities;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Repositories;

public sealed class InMemoryPasswordResetTokenRepositoryTests
{
    private readonly InMemoryPasswordResetTokenRepository _sut = new();

    [Fact]
    public async Task AddAsync_ThenFindByToken_ReturnsToken()
    {
        var token = PasswordResetToken.Create("alice@exam.io");
        await _sut.AddAsync(token);

        var found = await _sut.FindAsync(token.Token);
        found.Should().NotBeNull();
        found!.Email.Should().Be("alice@exam.io");
    }

    [Fact]
    public async Task FindAsync_UnknownToken_ReturnsNull()
    {
        var found = await _sut.FindAsync("does-not-exist");
        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsUsedAt()
    {
        var token = PasswordResetToken.Create("bob@exam.io");
        await _sut.AddAsync(token);

        token.MarkUsed();
        await _sut.UpdateAsync(token);

        var updated = await _sut.FindAsync(token.Token);
        updated.Should().NotBeNull();
        updated!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_TwoTokens_BothStoredIndependently()
    {
        var t1 = PasswordResetToken.Create("user1@exam.io");
        var t2 = PasswordResetToken.Create("user2@exam.io");

        await _sut.AddAsync(t1);
        await _sut.AddAsync(t2);

        (await _sut.FindAsync(t1.Token)).Should().NotBeNull();
        (await _sut.FindAsync(t2.Token)).Should().NotBeNull();
    }

    [Fact]
    public async Task Token_ExpiredToken_IsValidFalse()
    {
        var expired = PasswordResetToken.Create("c@exam.io",
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await _sut.AddAsync(expired);

        var found = await _sut.FindAsync(expired.Token);
        found!.IsValid.Should().BeFalse();
        found.IsExpired.Should().BeTrue();
    }
}
