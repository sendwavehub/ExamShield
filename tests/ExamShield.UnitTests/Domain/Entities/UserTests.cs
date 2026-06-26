using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class UserTests
{
    private static readonly Email TestEmail = new("admin@examshield.io");
    private const string TestHash = "$2a$04$hash";

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var user = User.Create(TestEmail, TestHash, UserRole.Administrator);

        user.Email.Should().Be(TestEmail);
        user.Role.Should().Be(UserRole.Administrator);
        user.IsActive.Should().BeTrue();
        user.Id.Value.Should().NotBe(Guid.Empty);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullEmail_Throws()
    {
        var act = () => User.Create(null!, TestHash, UserRole.Operator);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullOrEmptyHash_Throws()
    {
        var act = () => User.Create(TestEmail, null!, UserRole.Operator);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_StoresPasswordHashVerbatim()
    {
        const string hash = "$2a$04$abcdefghijklmnopqrstuuVGmFbkFDNVLBr0E5GjBnY8zf3wCe1Q2";
        var user = User.Create(TestEmail, hash, UserRole.Supervisor);
        user.PasswordHash.Should().Be(hash);
    }
}
