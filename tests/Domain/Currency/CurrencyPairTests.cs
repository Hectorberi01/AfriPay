using AfriPay.Domain.Currency;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Currency;

public sealed class CurrencyPairTests
{
    //Constructor

    [Fact]
    public void Constructor_ValidCodes_CreatesPair()
    {
        var pair = new CurrencyPair("EUR", "XOF");

        pair.From.Should().Be("EUR");
        pair.To.Should().Be("XOF");
    }

    [Fact]
    public void Constructor_NormalizesCodesToUpperCase()
    {
        var pair = new CurrencyPair("eur", "xof");

        pair.From.Should().Be("EUR");
        pair.To.Should().Be("XOF");
    }

    [Fact]
    public void Constructor_SameCurrency_ThrowsCurrencyDomainException()
    {
        var act = () => new CurrencyPair("XOF", "XOF");

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*different*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Constructor_InvalidFromCode_ThrowsCurrencyDomainException(string from)
    {
        var act = () => new CurrencyPair(from, "XOF");

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*Invalid source currency*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("XO")]
    [InlineData("XOFR")]
    public void Constructor_InvalidToCode_ThrowsCurrencyDomainException(string to)
    {
        var act = () => new CurrencyPair("EUR", to);

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*Invalid target currency*");
    }

    // Predefined pairs

    [Fact]
    public void EurXof_PredefinedPair_HasCorrectCurrencies()
    {
        CurrencyPair.EurXof.From.Should().Be("EUR");
        CurrencyPair.EurXof.To.Should().Be("XOF");
    }

    [Fact]
    public void UsdXof_PredefinedPair_HasCorrectCurrencies()
    {
        CurrencyPair.UsdXof.From.Should().Be("USD");
        CurrencyPair.UsdXof.To.Should().Be("XOF");
    }

    //Reverse

    [Fact]
    public void Reverse_EurXof_ReturnsXofEur()
    {
        var reversed = CurrencyPair.EurXof.Reverse();

        reversed.From.Should().Be("XOF");
        reversed.To.Should().Be("EUR");
    }

    // Contains 

    [Fact]
    public void Contains_FromCurrency_ReturnsTrue()
    {
        CurrencyPair.EurXof.Contains("EUR").Should().BeTrue();
    }

    [Fact]
    public void Contains_ToCurrency_ReturnsTrue()
    {
        CurrencyPair.EurXof.Contains("XOF").Should().BeTrue();
    }

    [Fact]
    public void Contains_UnrelatedCurrency_ReturnsFalse()
    {
        CurrencyPair.EurXof.Contains("USD").Should().BeFalse();
    }

    [Fact]
    public void Contains_IsCaseInsensitive()
    {
        CurrencyPair.EurXof.Contains("eur").Should().BeTrue();
    }

    // ToString 

    [Fact]
    public void ToString_ReturnsFromSlashTo()
    {
        new CurrencyPair("EUR", "XOF").ToString().Should().Be("EUR/XOF");
    }

    //Record equality

    [Fact]
    public void Equality_TwoPairsWithSameCurrencies_AreEqual()
    {
        var pair1 = new CurrencyPair("EUR", "XOF");
        var pair2 = new CurrencyPair("EUR", "XOF");

        pair1.Should().Be(pair2);
    }

    [Fact]
    public void Equality_PairsWithDifferentCurrencies_AreNotEqual()
    {
        var pair1 = new CurrencyPair("EUR", "XOF");
        var pair2 = new CurrencyPair("USD", "XOF");

        pair1.Should().NotBe(pair2);
    }
}