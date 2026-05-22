using AfriPay.Domain.Merchants;
using AfriPay.Domain.Merchants.ValueObjects;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Merchants;

public sealed class MerchantTests
{
    // ── Create ───────────────────────────────────────────────

    [Fact]
    public void Create_ValidArguments_ReturnsMerchantWithPendingStatus()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "contact@afrishop.sn", "SN");

        merchant.Status.Should().Be(MerchantStatus.Pending);
        merchant.BusinessName.Should().Be("AfriShop");
        merchant.Email.Should().Be("contact@afrishop.sn");
        merchant.Country.Should().Be("SN");
        merchant.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_NewMerchant_HasStarterPlanWithCorrectLimits()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "contact@afrishop.sn", "SN");

        merchant.Plan.Should().Be(PricingPlan.Starter);
        merchant.Limits.MonthlyTransactionLimit.Should().Be(500);
        merchant.Limits.RatePerMinute.Should().Be(100);
        merchant.Limits.CanAccessAllProviders.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesOneLiveAndOneSandboxApiKey()
    {
        var (merchant, liveKey, sandboxKey) = Merchant.Create("AfriShop", "contact@afrishop.sn", "SN");

        merchant.ApiKeys.Should().HaveCount(2);
        merchant.ApiKeys.Should().Contain(k => k.Type == ApiKeyType.Live);
        merchant.ApiKeys.Should().Contain(k => k.Type == ApiKeyType.Sandbox);
        liveKey.Should().StartWith("afp_live_");
        sandboxKey.Should().StartWith("afp_test_");
    }

    [Fact]
    public void Create_GeneratesUniqueKeysEachTime()
    {
        var (_, live1, _) = Merchant.Create("Shop1", "a@a.com", "SN");
        var (_, live2, _) = Merchant.Create("Shop2", "b@b.com", "CI");

        live1.Should().NotBe(live2);
    }

    // ── Verify ───────────────────────────────────────────────

    [Fact]
    public void Verify_SetsStatusToActiveAndRecordsVerifiedAt()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");
        var kyb = new KybInfo("AfriShop SARL", "SN123", "SN", DateOnly.FromDateTime(DateTime.UtcNow));

        var before = DateTimeOffset.UtcNow;
        merchant.Verify(kyb);

        merchant.Status.Should().Be(MerchantStatus.Active);
        merchant.VerifiedAt.Should().NotBeNull();
        merchant.VerifiedAt!.Value.Should().BeOnOrAfter(before);
        merchant.KybInfo.Should().Be(kyb);
    }

    // ── SetWebhook ───────────────────────────────────────────

    [Fact]
    public void SetWebhook_StoresUrlAndSecret()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");

        merchant.SetWebhook("https://shop.sn/webhooks", "s3cr3t");

        merchant.WebhookConfig.Should().NotBeNull();
        merchant.WebhookConfig!.Url.Should().Be("https://shop.sn/webhooks");
        merchant.WebhookConfig!.Secret.Should().Be("s3cr3t");
    }

    [Fact]
    public void SetWebhook_CalledTwice_ReplacesExistingConfig()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");
        merchant.SetWebhook("https://old.sn/hook", "old-secret");

        merchant.SetWebhook("https://new.sn/hook", "new-secret");

        merchant.WebhookConfig!.Url.Should().Be("https://new.sn/hook");
    }

    // ── UpgradePlan ──────────────────────────────────────────

    [Fact]
    public void UpgradePlan_ToGrowth_UpdatesPlanAndLimits()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");

        merchant.UpgradePlan(PricingPlan.Growth);

        merchant.Plan.Should().Be(PricingPlan.Growth);
        merchant.Limits.MonthlyTransactionLimit.Should().Be(5_000);
        merchant.Limits.RatePerMinute.Should().Be(1_000);
        merchant.Limits.CanAccessAllProviders.Should().BeTrue();
    }

    [Fact]
    public void UpgradePlan_ToScale_SetsUnlimitedLimits()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");

        merchant.UpgradePlan(PricingPlan.Scale);

        merchant.Limits.MonthlyTransactionLimit.Should().Be(int.MaxValue);
        merchant.Limits.RatePerMinute.Should().Be(int.MaxValue);
    }

    // ── Suspend ──────────────────────────────────────────────

    [Fact]
    public void Suspend_SetsStatusToSuspended()
    {
        var (merchant, _, _) = Merchant.Create("AfriShop", "a@a.com", "SN");
        merchant.Verify(new KybInfo("AfriShop SARL", "SN123", "SN", DateOnly.FromDateTime(DateTime.UtcNow)));

        merchant.Suspend();

        merchant.Status.Should().Be(MerchantStatus.Suspended);
    }
}