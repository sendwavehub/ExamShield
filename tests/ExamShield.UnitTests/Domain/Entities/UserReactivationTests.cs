using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class UserReactivationTests
{
    private static User ActiveUser() =>
        User.Create(new Email("test@exam.local"), "hash", UserRole.Operator);

    [Fact]
    public void Reactivate_DeactivatedUser_SetsIsActiveTrue()
    {
        var user = ActiveUser();
        user.Deactivate();

        user.Reactivate();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Reactivate_AlreadyActiveUser_ThrowsInvalidOperation()
    {
        var user = ActiveUser();

        Assert.Throws<InvalidOperationException>(() => user.Reactivate());
    }

    [Fact]
    public void Deactivate_ThenReactivate_ThenDeactivate_CyclesCorrectly()
    {
        var user = ActiveUser();

        user.Deactivate();
        user.Reactivate();
        user.Deactivate();

        Assert.False(user.IsActive);
    }
}
