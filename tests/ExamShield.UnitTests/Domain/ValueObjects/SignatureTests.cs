using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class SignatureTests
{
    [Fact]
    public void Create_WithNull_ThrowsArgumentNullException()
    {
        var act = () => new Signature(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("bytes");
    }

    [Fact]
    public void Create_WithEmptyBytes_ThrowsArgumentException()
    {
        var act = () => new Signature(Array.Empty<byte>());

        act.Should().Throw<ArgumentException>().WithParameterName("bytes");
    }

    [Fact]
    public void Create_WithValidBytes_Succeeds()
    {
        var bytes = new byte[64];
        bytes[0] = 0x30;

        var sig = new Signature(bytes);

        sig.Bytes.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public void Create_StoresDefensiveCopy()
    {
        var bytes = new byte[64];
        var sig = new Signature(bytes);

        bytes[0] = 0xFF;

        sig.Bytes[0].Should().Be(0x00);
    }

    [Fact]
    public void TwoSignatures_WithSameBytes_AreEqual()
    {
        var bytes = new byte[64];

        new Signature(bytes).Should().Be(new Signature(bytes));
    }

    [Fact]
    public void TwoSignatures_WithDifferentBytes_AreNotEqual()
    {
        var a = new byte[64];
        var b = new byte[64];
        b[0] = 0x01;

        new Signature(a).Should().NotBe(new Signature(b));
    }
}
