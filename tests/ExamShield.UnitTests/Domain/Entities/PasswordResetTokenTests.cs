using ExamShield.Domain.Entities;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class PasswordResetTokenTests
{
    [Fact]
    public void Create_SetsTokenAndEmail()
    {
        var token = PasswordResetToken.Create("User@Example.COM");

        Assert.NotEmpty(token.Token);
        Assert.Equal("user@example.com", token.Email); // normalised
    }

    [Fact]
    public void Create_DefaultExpiry_IsOneHourFromNow()
    {
        var before = DateTimeOffset.UtcNow;
        var token  = PasswordResetToken.Create("a@b.com");
        var after  = DateTimeOffset.UtcNow;

        Assert.InRange(token.ExpiresAt, before.AddHours(1).AddSeconds(-1), after.AddHours(1).AddSeconds(1));
    }

    [Fact]
    public void Create_NewToken_IsValid()
    {
        var token = PasswordResetToken.Create("a@b.com");

        Assert.True(token.IsValid);
        Assert.False(token.IsExpired);
        Assert.False(token.IsUsed);
    }

    [Fact]
    public void Create_ExpiredToken_IsNotValid()
    {
        var token = PasswordResetToken.Create("a@b.com", DateTimeOffset.UtcNow.AddHours(-1));

        Assert.False(token.IsValid);
        Assert.True(token.IsExpired);
    }

    [Fact]
    public void MarkUsed_SetsUsedAt()
    {
        var token = PasswordResetToken.Create("a@b.com");
        token.MarkUsed();

        Assert.True(token.IsUsed);
        Assert.False(token.IsValid);
        Assert.NotNull(token.UsedAt);
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordResetToken.Create(""));
    }
}
