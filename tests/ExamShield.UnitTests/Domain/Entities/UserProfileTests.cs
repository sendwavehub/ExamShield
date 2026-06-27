using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class UserProfileTests
{
    private static User Make() =>
        User.Create(new Email("profile@test.com"), "hash123", UserRole.Operator);

    [Fact]
    public void NewUser_DisplayName_IsNull()
    {
        var user = Make();
        Assert.Null(user.DisplayName);
    }

    [Fact]
    public void UpdateProfile_SetsDisplayName()
    {
        var user = Make();
        user.UpdateProfile("Alice Smith");
        Assert.Equal("Alice Smith", user.DisplayName);
    }

    [Fact]
    public void UpdateProfile_NullDisplayName_ClearsIt()
    {
        var user = Make();
        user.UpdateProfile("Bob");
        user.UpdateProfile(null);
        Assert.Null(user.DisplayName);
    }

    [Fact]
    public void UpdateProfile_WhitespaceOnly_Throws()
    {
        var user = Make();
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("   "));
    }

    [Fact]
    public void UpdateProfile_ExactlyMaxLength_Succeeds()
    {
        var user = Make();
        user.UpdateProfile(new string('A', 100));
        Assert.Equal(100, user.DisplayName!.Length);
    }

    [Fact]
    public void UpdateProfile_ExceedsMaxLength_Throws()
    {
        var user = Make();
        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateProfile(new string('A', 101)));
    }
}
