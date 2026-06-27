using ExamShield.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Security;

public sealed class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSecret_Returns32CharBase32String()
    {
        var secret = _sut.GenerateSecret();
        secret.Should().NotBeNullOrEmpty();
        secret.Length.Should().Be(32);
        secret.Should().MatchRegex("^[A-Z2-7]{32}$");
    }

    [Fact]
    public void GenerateSecret_ReturnsDifferentValuesEachCall()
    {
        var a = _sut.GenerateSecret();
        var b = _sut.GenerateSecret();
        a.Should().NotBe(b);
    }

    [Fact]
    public void GetQrUri_ContainsSecretAndEmail()
    {
        var secret = _sut.GenerateSecret();
        var uri = _sut.GetQrUri(secret, "test@exam.com");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain(secret);
        uri.Should().Contain("ExamShield");
    }

    [Fact]
    public void Verify_ReturnsTrueForCurrentCode()
    {
        var secret = _sut.GenerateSecret();
        var code = _sut.GenerateCurrentCode(secret);
        _sut.Verify(secret, code).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongCode()
    {
        var secret = _sut.GenerateSecret();
        _sut.Verify(secret, "000000").Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalseForNonNumericCode()
    {
        var secret = _sut.GenerateSecret();
        _sut.Verify(secret, "ABCDEF").Should().BeFalse();
    }

    [Fact]
    public void GenerateCurrentCode_Returns6DigitNumericString()
    {
        var secret = _sut.GenerateSecret();
        var code = _sut.GenerateCurrentCode(secret);
        code.Should().HaveLength(6);
        code.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public void GenerateCurrentCode_SameSecretSameWindowProducesSameCode()
    {
        var secret = _sut.GenerateSecret();
        var a = _sut.GenerateCurrentCode(secret);
        var b = _sut.GenerateCurrentCode(secret);
        a.Should().Be(b);
    }
}
