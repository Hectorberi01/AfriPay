using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Payments;

public sealed class PaymentTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly Money DefaultAmount = new(5000, "XOF");

    private static Payment CreatePending(
        string idempotencyKey = "key-001",
        string providerKey    = "mtn_momo") =>
        Payment.Create(MerchantId, idempotencyKey, DefaultAmount, providerKey, null, null, null);

    // ── Create ───────────────────────────────────────────────

    [Fact]
    public void Create_ValidArguments_ReturnsPaymentInPendingStatus()
    {
        var payment = CreatePending();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.MerchantId.Should().Be(MerchantId);
        payment.Amount.Should().Be(DefaultAmount);
        payment.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_SetsExpiryTo30MinutesInFuture()
    {
        var before  = DateTimeOffset.UtcNow;
        var payment = CreatePending();

        payment.ExpiresAt.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_AddsInitialTransitionToPending()
    {
        var payment = CreatePending();

        payment.Transitions.Should().HaveCount(1);
        payment.Transitions[0].ToStatus.Should().Be(PaymentStatus.Pending);
        payment.Transitions[0].FromStatus.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyIdempotencyKey_ThrowsDomainException()
    {
        var act = () => Payment.Create(MerchantId, "  ", DefaultAmount, "mtn_momo", null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*IdempotencyKey*");
    }

    [Fact]
    public void Create_EmptyProviderKey_ThrowsDomainException()
    {
        var act = () => Payment.Create(MerchantId, "key-001", DefaultAmount, "", null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*ProviderKey*");
    }

    // ── MarkCompleted ────────────────────────────────────────

    [Fact]
    public void MarkCompleted_PendingPayment_SetsStatusAndComputes()
    {
        var payment = CreatePending();

        payment.MarkCompleted("ref-123", "mtn_momo");

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ProviderReference.Should().Be("ref-123");
        payment.ProviderUsed.Should().Be("mtn_momo");
        payment.Fee.Should().NotBeNull();
        payment.Net.Should().NotBeNull();
        payment.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_FinalState_ThrowsDomainException()
    {
        var payment = CreatePending();
        payment.MarkCompleted("ref-1", "mtn_momo");

        var act = () => payment.MarkCompleted("ref-2", "mtn_momo");

        act.Should().Throw<DomainException>()
            .WithMessage("*final state*");
    }

    [Fact]
    public void MarkCompleted_NetEqualsAmountMinusFee()
    {
        var payment = CreatePending(providerKey: "mtn_momo");

        payment.MarkCompleted("ref-123", "mtn_momo");

        payment.Net!.Amount.Should().Be(payment.Amount.Amount - payment.Fee!.Amount);
    }

    // ── MarkFailed ───────────────────────────────────────────

    [Fact]
    public void MarkFailed_PendingPayment_SetsFailedStatus()
    {
        var payment = CreatePending();

        payment.MarkFailed("Provider timeout");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_FinalState_ThrowsDomainException()
    {
        var payment = CreatePending();
        payment.MarkFailed("error");

        var act = () => payment.MarkFailed("another error");

        act.Should().Throw<DomainException>();
    }

    // ── MarkCancelled ────────────────────────────────────────

    [Fact]
    public void MarkCancelled_PendingPayment_SetsCancelledStatus()
    {
        var payment = CreatePending();

        payment.MarkCancelled("merchant");

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void MarkCancelled_CompletedPayment_ThrowsDomainException()
    {
        var payment = CreatePending();
        payment.MarkCompleted("ref-1", "mtn_momo");

        var act = () => payment.MarkCancelled();

        act.Should().Throw<DomainException>()
            .WithMessage("*Pending*");
    }

    // ── MarkExpired ──────────────────────────────────────────

    [Fact]
    public void MarkExpired_PendingPayment_SetsExpiredStatus()
    {
        var payment = CreatePending();

        payment.MarkExpired();

        payment.Status.Should().Be(PaymentStatus.Expired);
        payment.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void MarkExpired_AlreadyCompletedPayment_IsNoOp()
    {
        var payment = CreatePending();
        payment.MarkCompleted("ref-1", "mtn_momo");

        payment.MarkExpired();

        payment.Status.Should().Be(PaymentStatus.Completed);
    }

    // ── MarkRefunded ─────────────────────────────────────────

    [Fact]
    public void MarkRefunded_CompletedPayment_SetsRefundedStatus()
    {
        var payment = CreatePending();
        payment.MarkCompleted("ref-1", "mtn_momo");

        payment.MarkRefunded();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_PendingPayment_ThrowsDomainException()
    {
        var payment = CreatePending();

        var act = () => payment.MarkRefunded();

        act.Should().Throw<DomainException>()
            .WithMessage("*Completed*");
    }

    // ── RecordProviderAttempt ────────────────────────────────

    [Fact]
    public void RecordProviderAttempt_Success_SetsProviderUsed()
    {
        var payment = CreatePending();

        payment.RecordProviderAttempt("mtn_momo", "ref-abc", success: true, null);

        payment.ProviderUsed.Should().Be("mtn_momo");
        payment.ProviderReference.Should().Be("ref-abc");
        payment.Attempts.Should().HaveCount(1);
        payment.Attempts[0].Success.Should().BeTrue();
    }

    [Fact]
    public void RecordProviderAttempt_Failure_DoesNotSetProviderUsed()
    {
        var payment = CreatePending();

        payment.RecordProviderAttempt("mtn_momo", null, success: false, "TIMEOUT");

        payment.ProviderUsed.Should().BeNull();
        payment.Attempts[0].Success.Should().BeFalse();
    }

    [Fact]
    public void RecordProviderAttempt_MultipleAttempts_AllRecorded()
    {
        var payment = CreatePending();

        payment.RecordProviderAttempt("mtn_momo", null, success: false, "ERR_1");
        payment.RecordProviderAttempt("wave", "ref-ok", success: true, null);

        payment.Attempts.Should().HaveCount(2);
    }

    // ── SetUssdCode ──────────────────────────────────────────

    [Fact]
    public void SetUssdCode_SetsTheCode()
    {
        var payment = CreatePending();

        payment.SetUssdCode("*126*1*1#");

        payment.UssdCode.Should().Be("*126*1*1#");
    }

    // ── IsFinalState / IsExpired ─────────────────────────────

    [Theory]
    [InlineData("completed")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    [InlineData("expired")]
    public void IsFinalState_ReturnsTrue_ForAllFinalStatuses(string statusName)
    {
        var payment = CreatePending();

        switch (statusName)
        {
            case "completed":  payment.MarkCompleted("ref", "mtn_momo"); break;
            case "failed":     payment.MarkFailed("err");                break;
            case "cancelled":  payment.MarkCancelled();                  break;
            case "expired":    payment.MarkExpired();                    break;
        }

        payment.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void IsFinalState_Pending_ReturnsFalse()
    {
        var payment = CreatePending();

        payment.IsFinalState.Should().BeFalse();
    }
}