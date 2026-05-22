using AfriPay.Domain.Merchants;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Merchants;

public sealed class ApiKeyTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();

    // CreateLive

    [Fact]
    public void CreateLive_ReturnsLiveKeyWithCorrectPrefix()
    {
        var (rawKey, entity) = ApiKey.CreateLive(MerchantId);

        rawKey.Should().StartWith("afp_live_");
        entity.Type.Should().Be(ApiKeyType.Live);
        entity.Prefix.Should().Be("afp_live_");
    }

    [Fact]
    public void CreateLive_EntityIsActive()
    {
        var (_, entity) = ApiKey.CreateLive(MerchantId);

        entity.IsActive.Should().BeTrue();
        entity.RevokedAt.Should().BeNull();
        entity.MerchantId.Should().Be(MerchantId);
    }

    [Fact]
    public void CreateLive_KeyHashIsSha256OfRawKey()
    {
        var (rawKey, entity) = ApiKey.CreateLive(MerchantId);

        var expectedHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();

        entity.KeyHash.Should().Be(expectedHash);
    }

    // CreateSandbox 

    [Fact]
    public void CreateSandbox_ReturnsTestKeyWithCorrectPrefix()
    {
        var (rawKey, entity) = ApiKey.CreateSandbox(MerchantId);

        rawKey.Should().StartWith("afp_test_");
        entity.Type.Should().Be(ApiKeyType.Sandbox);
        entity.Prefix.Should().Be("afp_test_");
    }

    //  Uniqueness 

    [Fact]
    public void CreateLive_TwoCallsReturnDifferentKeys()
    {
        var (key1, _) = ApiKey.CreateLive(MerchantId);
        var (key2, _) = ApiKey.CreateLive(MerchantId);

        key1.Should().NotBe(key2);
    }

    // GetRawKeyOnce 

    [Fact]
    public void GetRawKeyOnce_AfterFactoryCreation_ThrowsBecauseRawKeyExposedViaTuple()
    {
        // The factory returns the raw key directly via the tuple.
        // The internal _rawKey field is not populated, so GetRawKeyOnce
        // is not usable after factory creation — it always throws.
        var (_, entity) = ApiKey.CreateLive(MerchantId);

        var act = () => entity.GetRawKeyOnce();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only be retrieved once*");
    }

    // Revoke

    [Fact]
    public void Revoke_ActiveKey_SetsInactiveAndRecordsRevokedAt()
    {
        var (_, entity) = ApiKey.CreateLive(MerchantId);
        var before = DateTimeOffset.UtcNow;

        entity.Revoke();

        entity.IsActive.Should().BeFalse();
        entity.RevokedAt.Should().NotBeNull();
        entity.RevokedAt!.Value.Should().BeOnOrAfter(before);
    }
}