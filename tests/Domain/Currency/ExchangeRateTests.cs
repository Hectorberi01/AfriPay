using AfriPay.Domain.Currency;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Currency;

public sealed class ExchangeRateTests
{
    // ── Record ───────────────────────────────────────────────

    [Fact]
    public void Record_ValidArguments_CreatesExchangeRate()
    {
        var rate = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.005m, RateSource.Fixed);

        rate.Pair.Should().Be(CurrencyPair.EurXof);
        rate.OfficialRate.Should().Be(655.957m);
        rate.Spread.Should().Be(0.005m);
        rate.Source.Should().Be(RateSource.Fixed);
        rate.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Record_ComputesEffectiveRateAfterSpread()
    {
        var rate = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.005m, RateSource.Fixed);

        // EffectiveRate = 655.957 × (1 - 0.005) = 652.677...
        var expected = decimal.Round(655.957m * (1 - 0.005m), 8);
        rate.EffectiveRate.Should().Be(expected);
    }

    [Fact]
    public void Record_ZeroSpread_EffectiveRateEqualsOfficialRate()
    {
        var rate = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0m, RateSource.Fixed);

        rate.EffectiveRate.Should().Be(rate.OfficialRate);
    }

    [Fact]
    public void Record_NegativeOfficialRate_ThrowsCurrencyDomainException()
    {
        var act = () => ExchangeRate.Record(CurrencyPair.EurXof, -1m, 0.005m, RateSource.Fixed);

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void Record_ZeroOfficialRate_ThrowsCurrencyDomainException()
    {
        var act = () => ExchangeRate.Record(CurrencyPair.EurXof, 0m, 0m, RateSource.Fixed);

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void Record_SpreadAbove2Percent_ThrowsCurrencyDomainException()
    {
        var act = () => ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.021m, RateSource.Fixed);

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*2%*");
    }

    [Fact]
    public void Record_NegativeSpread_ThrowsCurrencyDomainException()
    {
        var act = () => ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, -0.001m, RateSource.Fixed);

        act.Should().Throw<CurrencyDomainException>()
            .WithMessage("*between 0%*");
    }

    [Fact]
    public void Record_SetsValidUntilFromValidForMinutes()
    {
        var before = DateTimeOffset.UtcNow;
        var rate   = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0m, RateSource.Fixed, validForMinutes: 60);

        rate.ValidUntil.Should().BeCloseTo(before.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    // ── CreateEurXofFixed ────────────────────────────────────

    [Fact]
    public void CreateEurXofFixed_UsesOfficialFixedRate()
    {
        var rate = ExchangeRate.CreateEurXofFixed();

        rate.OfficialRate.Should().Be(ExchangeRate.EurXofFixedRate);
        rate.Pair.Should().Be(CurrencyPair.EurXof);
        rate.Source.Should().Be(RateSource.Fixed);
    }

    [Fact]
    public void CreateEurXofFixed_DefaultSpread_Is05Percent()
    {
        var rate = ExchangeRate.CreateEurXofFixed();

        rate.Spread.Should().Be(0.005m);
    }

    [Fact]
    public void CreateEurXofFixed_ValidForOneYear()
    {
        var before = DateTimeOffset.UtcNow;
        var rate   = ExchangeRate.CreateEurXofFixed();

        rate.ValidUntil.Should().BeCloseTo(before.AddYears(1), TimeSpan.FromSeconds(5));
    }

    // ── Convert ──────────────────────────────────────────────

    [Fact]
    public void Convert_UsesEffectiveRateWithFloor()
    {
        var rate   = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.005m, RateSource.Fixed);
        var amount = 1L;   // 1 EUR

        var result = rate.Convert(amount);

        var expected = (long)Math.Floor(amount * rate.EffectiveRate);
        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_100Eur_GivesExpectedXofAmount()
    {
        var rate   = ExchangeRate.CreateEurXofFixed(spread: 0m);
        var result = rate.Convert(100);

        result.Should().Be((long)Math.Floor(100 * ExchangeRate.EurXofFixedRate));
    }

    // ── ConvertAtOfficialRate ────────────────────────────────

    [Fact]
    public void ConvertAtOfficialRate_IgnoresSpread()
    {
        var rate   = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.01m, RateSource.Fixed);
        var amount = 100L;

        var official  = rate.ConvertAtOfficialRate(amount);
        var effective = rate.Convert(amount);

        official.Should().BeGreaterThan(effective);
    }

    // ── SpreadAmount ─────────────────────────────────────────

    [Fact]
    public void SpreadAmount_WithNonZeroSpread_IsPositive()
    {
        var rate   = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0.005m, RateSource.Fixed);

        var spread = rate.SpreadAmount(100);

        spread.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SpreadAmount_ZeroSpread_IsZero()
    {
        var rate  = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0m, RateSource.Fixed);

        rate.SpreadAmount(100).Should().Be(0);
    }

    // ── IsExpired ────────────────────────────────────────────

    [Fact]
    public void IsExpired_FreshRate_ReturnsFalse()
    {
        var rate = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0m, RateSource.Fixed, validForMinutes: 60);

        rate.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ExpiredRate_ReturnsTrue()
    {
        // validForMinutes = 0 means ValidUntil = now, which is immediately past
        var rate = ExchangeRate.Record(CurrencyPair.EurXof, 655.957m, 0m, RateSource.Fixed, validForMinutes: -1);

        rate.IsExpired.Should().BeTrue();
    }
}