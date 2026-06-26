using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class PublicKeyTests
{
    [Fact]
    public void Constructor_WithValidBytes_Succeeds()
    {
        var key = new PublicKey(new byte[] { 0x04, 0x01 });

        key.Bytes.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithEmptyBytes_ThrowsArgumentException()
    {
        var act = () => new PublicKey(Array.Empty<byte>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullBytes_ThrowsArgumentNullException()
    {
        var act = () => new PublicKey(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_MakesDefensiveCopy()
    {
        var original = new byte[] { 0x01, 0x02 };
        var key = new PublicKey(original);

        original[0] = 0xFF;

        key.Bytes[0].Should().Be(0x01);
    }

    [Fact]
    public void Equals_WithIdenticalBytes_ReturnsTrue()
    {
        var a = new PublicKey(new byte[] { 0x01, 0x02 });
        var b = new PublicKey(new byte[] { 0x01, 0x02 });

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_WithDifferentBytes_ReturnsFalse()
    {
        var a = new PublicKey(new byte[] { 0x01 });
        var b = new PublicKey(new byte[] { 0x02 });

        a.Should().NotBe(b);
    }
}
