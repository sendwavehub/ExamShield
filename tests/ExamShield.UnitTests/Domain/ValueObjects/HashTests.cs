using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class HashTests
{
    private static readonly string ValidHex = new('a', 64);

    [Fact]
    public void FromHex_WithNull_ThrowsArgumentException()
    {
        var act = () => Hash.FromHex(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromHex_WithWrongLength_ThrowsArgumentException()
    {
        var act = () => Hash.FromHex(new string('a', 63));

        act.Should().Throw<ArgumentException>().WithParameterName("hex");
    }

    [Fact]
    public void FromHex_WithInvalidCharacters_ThrowsArgumentException()
    {
        var act = () => Hash.FromHex(new string('z', 64));

        act.Should().Throw<ArgumentException>().WithParameterName("hex");
    }

    [Fact]
    public void FromHex_WithValidHex_StoresLowercaseHex()
    {
        var hash = Hash.FromHex(ValidHex.ToUpperInvariant());

        hash.Hex.Should().Be(ValidHex);
    }

    [Fact]
    public void FromBytes_WithNull_ThrowsArgumentNullException()
    {
        var act = () => Hash.FromBytes(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromBytes_WithWrongLength_ThrowsArgumentException()
    {
        var act = () => Hash.FromBytes(new byte[16]);

        act.Should().Throw<ArgumentException>().WithParameterName("bytes");
    }

    [Fact]
    public void FromBytes_WithValid32Bytes_ProducesHex()
    {
        var bytes = new byte[32];
        bytes[0] = 0xAB;

        var hash = Hash.FromBytes(bytes);

        hash.Hex.Should().StartWith("ab");
        hash.Hex.Should().HaveLength(64);
    }

    [Fact]
    public void TwoHashes_WithSameHex_AreEqual()
    {
        Hash.FromHex(ValidHex).Should().Be(Hash.FromHex(ValidHex));
    }

    [Fact]
    public void TwoHashes_WithDifferentHex_AreNotEqual()
    {
        Hash.FromHex(new string('a', 64)).Should().NotBe(Hash.FromHex(new string('b', 64)));
    }
}
