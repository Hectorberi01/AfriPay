using AfriPay.Domain.Webhooks;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Webhooks;

public sealed class WebhookDeliveryTests
{
    private static readonly Guid   MerchantId = Guid.NewGuid();
    private const string           Secret     = "super-secret-key";
    private const string           TargetUrl  = "https://merchant.example.com/webhooks";

    private static WebhookPayload BuildPayload() => new()
    {
        EventId     = Guid.NewGuid().ToString(),
        EventType   = "payment.completed",
        ProviderKey = "mtn_momo",
        PaymentId   = Guid.NewGuid().ToString(),
        Status      = "completed",
        Amount      = 5_000,
        Currency    = "XOF",
        OccurredAt  = DateTimeOffset.UtcNow,
    };

    private static WebhookDelivery CreateDelivery(string? url = TargetUrl) =>
        WebhookDelivery.Create(MerchantId, "payment.completed", BuildPayload(), Secret, url ?? TargetUrl);

    // Create 

    [Fact]
    public void Create_ValidArguments_ReturnsPendingDelivery()
    {
        var delivery = CreateDelivery();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.MerchantId.Should().Be(MerchantId);
        delivery.TargetUrl.Should().Be(TargetUrl);
        delivery.AttemptCount.Should().Be(0);
        delivery.Signature.Should().StartWith("sha256=");
        delivery.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_EmptyTargetUrl_ThrowsWebhookDomainException()
    {
        var act = () => WebhookDelivery.Create(MerchantId, "payment.completed", BuildPayload(), Secret, "");

        act.Should().Throw<WebhookDomainException>()
            .WithMessage("*TargetUrl*");
    }

    [Fact]
    public void Create_InvalidUrl_ThrowsWebhookDomainException()
    {
        var act = () => WebhookDelivery.Create(MerchantId, "payment.completed", BuildPayload(), Secret, "not-a-url");

        act.Should().Throw<WebhookDomainException>()
            .WithMessage("*valid HTTPS URL*");
    }

    [Fact]
    public void Create_HttpNotHttpsUrl_IsAccepted()
    {
        // Http is allowed (allows local dev/test environments)
        var act = () => WebhookDelivery.Create(MerchantId, "payment.completed", BuildPayload(), Secret, "http://localhost/hook");

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_SignatureMatchesComputedHmac()
    {
        var payload   = BuildPayload();
        var json      = payload.ToJson();
        var expected  = WebhookDelivery.ComputeSignature(json, Secret);
        var delivery  = WebhookDelivery.Create(MerchantId, "payment.completed", payload, Secret, TargetUrl);

        delivery.Signature.Should().Be(expected);
    }

    // RecordAttempt — success 

    [Fact]
    public void RecordAttempt_SuccessfulHttpResponse_SetsDeliveredStatus()
    {
        var delivery = CreateDelivery();

        delivery.RecordAttempt(200, "OK", 120);

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        delivery.AttemptCount.Should().Be(1);
        delivery.DeliveredAt.Should().NotBeNull();
        delivery.NextRetryAt.Should().BeNull();
        delivery.IsTerminal.Should().BeTrue();
    }

    [Theory]
    [InlineData(201)]
    [InlineData(204)]
    public void RecordAttempt_2xxStatusCodes_SetDelivered(int statusCode)
    {
        var delivery = CreateDelivery();

        delivery.RecordAttempt(statusCode, null, 50);

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
    }

    // RecordAttempt — failure / retry 

    [Fact]
    public void RecordAttempt_FailedHttpResponse_SetsRetryingStatusWithNextRetry()
    {
        var delivery = CreateDelivery();

        delivery.RecordAttempt(503, "Service Unavailable", 200);

        delivery.Status.Should().Be(DeliveryStatus.Retrying);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextRetryAt.Should().NotBeNull();
        delivery.NextRetryAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RecordAttempt_MaxAttemptsReached_SetsDeadLetterStatus()
    {
        var delivery = CreateDelivery();

        for (var i = 0; i < RetrySchedule.MaxAttempts; i++)
            delivery.RecordAttempt(500, "Internal Server Error", 100);

        delivery.Status.Should().Be(DeliveryStatus.DeadLetter);
        delivery.NextRetryAt.Should().BeNull();
        delivery.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void RecordAttempt_OnTerminalDelivery_ThrowsWebhookDomainException()
    {
        var delivery = CreateDelivery();
        delivery.RecordAttempt(200, "OK", 50);

        var act = () => delivery.RecordAttempt(200, "OK", 50);

        act.Should().Throw<WebhookDomainException>()
            .WithMessage("*Delivered*");
    }

    [Fact]
    public void RecordAttempt_TracksAllAttempts()
    {
        var delivery = CreateDelivery();

        delivery.RecordAttempt(500, "err", 100);
        delivery.RecordAttempt(503, "unavailable", 200);

        delivery.Attempts.Should().HaveCount(2);
        delivery.Attempts[0].Success.Should().BeFalse();
        delivery.Attempts[1].Success.Should().BeFalse();
    }

    // ResetForManualRetry 

    [Fact]
    public void ResetForManualRetry_DeadLetterDelivery_ResetsToInitialState()
    {
        var delivery = CreateDelivery();
        for (var i = 0; i < RetrySchedule.MaxAttempts; i++)
            delivery.RecordAttempt(500, "err", 100);

        delivery.ResetForManualRetry();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void ResetForManualRetry_PendingDelivery_ThrowsWebhookDomainException()
    {
        var delivery = CreateDelivery();

        var act = () => delivery.ResetForManualRetry();

        act.Should().Throw<WebhookDomainException>()
            .WithMessage("*dead-letter*");
    }

    [Fact]
    public void ResetForManualRetry_DeliveredDelivery_ThrowsWebhookDomainException()
    {
        var delivery = CreateDelivery();
        delivery.RecordAttempt(200, "OK", 50);

        var act = () => delivery.ResetForManualRetry();

        act.Should().Throw<WebhookDomainException>();
    }

    // ComputeSignature / VerifySignature

    [Fact]
    public void ComputeSignature_ReturnsSha256PrefixedHex()
    {
        var sig = WebhookDelivery.ComputeSignature("{\"test\":true}", Secret);

        sig.Should().StartWith("sha256=");
        sig.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void ComputeSignature_SamePayloadAndSecret_ReturnsSameSignature()
    {
        var sig1 = WebhookDelivery.ComputeSignature("payload", Secret);
        var sig2 = WebhookDelivery.ComputeSignature("payload", Secret);

        sig1.Should().Be(sig2);
    }

    [Fact]
    public void VerifySignature_CorrectSignature_ReturnsTrue()
    {
        var payload = "{\"event\":\"payment.completed\"}";
        var sig     = WebhookDelivery.ComputeSignature(payload, Secret);

        var result = WebhookDelivery.VerifySignature(payload, sig, Secret);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_WrongSecret_ReturnsFalse()
    {
        var payload = "{\"event\":\"payment.completed\"}";
        var sig     = WebhookDelivery.ComputeSignature(payload, Secret);

        var result = WebhookDelivery.VerifySignature(payload, sig, "wrong-secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_TamperedPayload_ReturnsFalse()
    {
        var payload  = "{\"event\":\"payment.completed\"}";
        var tampered = "{\"event\":\"payment.refunded\"}";
        var sig      = WebhookDelivery.ComputeSignature(payload, Secret);

        var result = WebhookDelivery.VerifySignature(tampered, sig, Secret);

        result.Should().BeFalse();
    }

    // IsDue 

    [Fact]
    public void IsDue_NewPendingDelivery_ReturnsTrue()
    {
        var delivery = CreateDelivery();

        delivery.IsDue.Should().BeTrue();
    }

    [Fact]
    public void IsDue_DeliveredDelivery_ReturnsFalse()
    {
        var delivery = CreateDelivery();
        delivery.RecordAttempt(200, "OK", 50);

        delivery.IsDue.Should().BeFalse();
    }
}