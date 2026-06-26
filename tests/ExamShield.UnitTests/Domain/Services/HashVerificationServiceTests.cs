using System.Security.Cryptography;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Services;

public sealed class HashVerificationServiceTests
{
    private readonly HashVerificationService _sut = new();

    [Fact]
    public void ComputeHash_WithNullBytes_ThrowsArgumentNullException()
    {
        var act = () => _sut.ComputeHash(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("imageBytes");
    }

    [Fact]
    public void ComputeHash_WithEmptyBytes_ThrowsArgumentException()
    {
        var act = () => _sut.ComputeHash(Array.Empty<byte>());

        act.Should().Throw<ArgumentException>().WithParameterName("imageBytes");
    }

    [Fact]
    public void ComputeHash_WithBytes_ReturnsSha256Hash()
    {
        var imageBytes = "hello world"u8.ToArray();
        var expected = SHA256.HashData(imageBytes);

        var hash = _sut.ComputeHash(imageBytes);

        hash.Should().Be(Hash.FromBytes(expected));
    }

    [Fact]
    public void ComputeHash_SameInput_ProducesSameHash()
    {
        var imageBytes = new byte[] { 1, 2, 3 };

        _sut.ComputeHash(imageBytes).Should().Be(_sut.ComputeHash(imageBytes));
    }

    [Fact]
    public void ComputeHash_DifferentInput_ProducesDifferentHash()
    {
        _sut.ComputeHash(new byte[] { 1 }).Should().NotBe(_sut.ComputeHash(new byte[] { 2 }));
    }

    [Fact]
    public void Verify_WhenHashesMatch_ReturnsTrue()
    {
        var imageBytes = new byte[] { 10, 20, 30 };
        var hash = _sut.ComputeHash(imageBytes);

        _sut.Verify(imageBytes, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WhenHashesDiffer_ReturnsFalse()
    {
        var hash = _sut.ComputeHash(new byte[] { 1, 2, 3 });

        _sut.Verify(new byte[] { 9, 9, 9 }, hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithNullBytes_ThrowsArgumentNullException()
    {
        var hash = _sut.ComputeHash(new byte[] { 1 });
        var act = () => _sut.Verify(null!, hash);

        act.Should().Throw<ArgumentNullException>().WithParameterName("imageBytes");
    }

    [Fact]
    public void Verify_WithNullHash_ThrowsArgumentNullException()
    {
        var act = () => _sut.Verify(new byte[] { 1 }, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("expectedHash");
    }
}
