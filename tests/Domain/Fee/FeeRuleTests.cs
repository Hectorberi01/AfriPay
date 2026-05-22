using AfriPay.Domain.Fee;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Fee;

public sealed class FeeRuleTests
{
    // CreatePercentage 

    [Fact]
    public void CreatePercentage_ValidArguments_CreatesActiveRule()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.ProviderKey.Should().Be("mtn_momo");
        rule.Plan.Should().Be("starter");
        rule.Currency.Should().Be("XOF");
        rule.RatePercent.Should().Be(0.8m);
        rule.Type.Should().Be(FeeType.Percentage);
        rule.IsActive.Should().BeTrue();
        rule.Id.Should().NotBeEmpty();
    }

    // CreateMixed 

    [Fact]
    public void CreateMixed_ValidArguments_CreatesMixedRule()
    {
        var rule = FeeRule.CreateMixed("stripe", "growth", "EUR", 1.2m, 30, FeeTier.MobileMoney);

        rule.Type.Should().Be(FeeType.Mixed);
        rule.RatePercent.Should().Be(1.2m);
        rule.FixedAmount.Should().Be(30);
    }

    // Calculate — Percentage 

    [Fact]
    public void Calculate_PercentageType_ReturnsCorrectFee()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        var fee = rule.Calculate(10_000);

        // 0.8% of 10 000 = 80
        fee.Should().Be(80);
    }

    [Fact]
    public void Calculate_PercentageType_CeilsDecimalResult()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        // 0.8% of 1250 = 10.0 — exact
        var fee = rule.Calculate(1250);

        fee.Should().Be(10);
    }

    [Fact]
    public void Calculate_PercentageFractional_CeilsUp()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        // 0.8% of 100 = 0.8 → ceiling = 1
        var fee = rule.Calculate(100);

        fee.Should().Be(1);
    }

    //Calculate — Mixed

    [Fact]
    public void Calculate_MixedType_SumsPercentageAndFixed()
    {
        var rule = FeeRule.CreateMixed("stripe", "starter", "EUR", 1.2m, 30, FeeTier.MobileMoney);

        // 1.2% of 10 000 = 120, + 30 fixed = 150
        var fee = rule.Calculate(10_000);

        fee.Should().Be(150);
    }

    // Calculate — MaxFee cap 

    [Fact]
    public void Calculate_PercentageWithMaxFee_IsCappedAtMax()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney, maxFee: 1_000);

        // 0.8% of 1 000 000 = 8 000 — capped at 1 000
        var fee = rule.Calculate(1_000_000);

        fee.Should().Be(1_000);
    }

    [Fact]
    public void Calculate_MixedWithMaxFee_IsCappedAtMax()
    {
        var rule = FeeRule.CreateMixed("stripe", "starter", "EUR", 1.2m, 30, FeeTier.MobileMoney, maxFee: 200);

        // 1.2% of 50 000 = 600 + 30 = 630 — capped at 200
        var fee = rule.Calculate(50_000);

        fee.Should().Be(200);
    }

    // ── AppliesTo ────────────────────────────────────────────

    [Fact]
    public void AppliesTo_MatchingRule_ReturnsTrue()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("mtn_momo", "starter", "XOF", 5_000).Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_InactiveRule_ReturnsFalse()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);
        rule.Deactivate();

        rule.AppliesTo("mtn_momo", "starter", "XOF", 5_000).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_WildcardProvider_MatchesAnyProvider()
    {
        var rule = FeeRule.CreatePercentage("*", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("stripe", "starter", "XOF", 5_000).Should().BeTrue();
        rule.AppliesTo("mtn_momo", "starter", "XOF", 5_000).Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_WildcardCurrency_MatchesAnyCurrency()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "*", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("mtn_momo", "starter", "XOF", 5_000).Should().BeTrue();
        rule.AppliesTo("mtn_momo", "starter", "EUR", 5_000).Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_WrongProvider_ReturnsFalse()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("stripe", "starter", "XOF", 5_000).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_WrongPlan_ReturnsFalse()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("mtn_momo", "growth", "XOF", 5_000).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_AmountBelowMinAmount_ReturnsFalse()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney,
            minAmount: 1_000);

        rule.AppliesTo("mtn_momo", "starter", "XOF", 500).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_AmountAboveMaxAmount_ReturnsFalse()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney,
            maxAmount: 50_000);

        rule.AppliesTo("mtn_momo", "starter", "XOF", 100_000).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_MaxAmountZero_MeansNoCap()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney,
            maxAmount: 0);

        rule.AppliesTo("mtn_momo", "starter", "XOF", 1_000_000).Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_CaseInsensitiveComparison()
    {
        var rule = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.AppliesTo("MTN_MOMO", "STARTER", "xof", 5_000).Should().BeTrue();
    }

    // ── Deactivate ───────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsInactiveAndEffectiveTo()
    {
        var before = DateTimeOffset.UtcNow;
        var rule   = FeeRule.CreatePercentage("mtn_momo", "starter", "XOF", 0.8m, FeeTier.MobileMoney);

        rule.Deactivate();

        rule.IsActive.Should().BeFalse();
        rule.EffectiveTo.Should().NotBeNull();
        rule.EffectiveTo!.Value.Should().BeOnOrAfter(before);
    }
}
