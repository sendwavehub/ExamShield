using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class RefreshTokenTests
{
    private static UserId NewUser() => new(Guid.NewGuid());

    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var userId = NewUser();
        var token = RefreshToken.Create(userId, "hash-abc", 7);

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be("hash-abc");
        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ExpiresAt_IsAfterNow()
    {
        var token = RefreshToken.Create(NewUser(), "h", 14);
        token.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_SetsIsRevoked()
    {
        var token = RefreshToken.Create(NewUser(), "h", 7);
        token.Revoke();
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_ZeroExpiryDays_ExpiresImmediately()
    {
        var token = RefreshToken.Create(NewUser(), "h", 0);
        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var a = RefreshToken.Create(NewUser(), "h1", 7);
        var b = RefreshToken.Create(NewUser(), "h2", 7);
        a.Id.Should().NotBe(b.Id);
    }
}
