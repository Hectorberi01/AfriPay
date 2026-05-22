using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Payments.ValueObjects;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Payments;

public sealed class MoneyTests
{
    // Constructor

    [Fact]
    public void Constructor_ValidArguments_CreatesInstance()
    {
        var money = new Money(5000, "XOF");

        money.Amount.Should().Be(5000);
        money.Currency.Should().Be("XOF");
    }

    [Fact]
    public void Constructor_NormalizesUpperCaseCurrency()
    {
        var money = new Money(1200, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsDomainException()
    {
        var act = () => new Money(-1, "XOF");

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Constructor_InvalidCurrency_ThrowsDomainException(string currency)
    {
        var act = () => new Money(100, currency);

        act.Should().Throw<DomainException>()
            .WithMessage("*ISO 4217*");
    }

    // CalculateFee 

    [Fact]
    public void CalculateFee_StripeProvider_Applies12PercentRate()
    {
        var amount = new Money(10_000, "XOF");

        var fee = Money.CalculateFee(amount, "stripe");

        // 1.2% of 10 000 = 120, above minimum 50
        fee.Amount.Should().Be(120);
        fee.Currency.Should().Be("XOF");
    }

    [Fact]
    public void CalculateFee_PaypalProvider_Applies12PercentRate()
    {
        var amount = new Money(10_000, "XOF");

        var fee = Money.CalculateFee(amount, "paypal");

        fee.Amount.Should().Be(120);
    }

    [Fact]
    public void CalculateFee_MobilMoneyProvider_Applies08PercentRate()
    {
        var amount = new Money(10_000, "XOF");

        var fee = Money.CalculateFee(amount, "mtn_momo");

        // 0.8% of 10 000 = 80
        fee.Amount.Should().Be(80);
    }

    [Fact]
    public void CalculateFee_TinyXofAmount_ReturnsMinimum50()
    {
        var amount = new Money(100, "XOF");

        var fee = Money.CalculateFee(amount, "mtn_momo");

        // 0.8% of 100 = 0.8, ceiling = 1, minimum XOF = 50
        fee.Amount.Should().Be(50);
    }

    [Fact]
    public void CalculateFee_EurAmount_ReturnsMinimum1Centime()
    {
        var amount = new Money(10, "EUR");

        var fee = Money.CalculateFee(amount, "mtn_momo");

        // 0.8% of 10 = 0.08, ceiling = 1 — minimum EUR = 1
        fee.Amount.Should().Be(1);
    }

    //Subtract 

    [Fact]
    public void Subtract_SameCurrency_ReturnsCorrectDifference()
    {
        var a = new Money(1000, "XOF");
        var b = new Money(200, "XOF");

        var result = a.Subtract(b);

        result.Amount.Should().Be(800);
        result.Currency.Should().Be("XOF");
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ThrowsDomainException()
    {
        var a = new Money(1000, "XOF");
        var b = new Money(200, "EUR");

        var act = () => a.Subtract(b);

        act.Should().Throw<DomainException>();
    }

    // Zero 

    [Fact]
    public void Zero_ReturnsZeroAmountForGivenCurrency()
    {
        var zero = Money.Zero("XOF");

        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("XOF");
    }

    // ToString 

    [Fact]
    public void ToString_XofAmount_FormatsWithoutDecimals()
    {
        var money = new Money(5000, "XOF");

        money.ToString().Should().Contain("XOF");
    }

    [Fact]
    public void ToString_EurAmount_FormatsAsDecimal()
    {
        var money = new Money(1200, "EUR");

        money.ToString().Should().Contain("EUR");
    }
}