using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("admin@examshield.io")]
    [InlineData("x@y.co")]
    public void Constructor_WithValidEmail_SetsValue(string address)
    {
        var email = new Email(address);
        email.Value.Should().Be(address.ToLowerInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespace_Throws(string? address)
    {
        var act = () => new Email(address!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    [InlineData("noatsign.com")]
    public void Constructor_WithInvalidFormat_Throws(string address)
    {
        var act = () => new Email(address);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NormalisesAddressToLowercase()
    {
        var email = new Email("Admin@ExamShield.IO");
        email.Value.Should().Be("admin@examshield.io");
    }

    [Fact]
    public void Equality_SameAddress_AreEqual()
    {
        new Email("a@b.com").Should().Be(new Email("A@B.COM"));
    }
}
