using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Refunds;
using FluentAssertions;
using DomainException = AfriPay.Domain.Refunds.DomainException;

namespace AfriPay.Tests.Domain.Refunds;

public sealed class RefundTests
{
    private static readonly Guid PaymentId  = Guid.NewGuid();
    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly Money PaymentAmount = new(10_000, "XOF");

    private static Refund CreatePending(
        Money?  refundAmount = null,
        string  providerKey  = "mtn_momo",
        string? notes        = null) =>
        Refund.Create(PaymentId, MerchantId, PaymentAmount, refundAmount,
            RefundReason.CustomerRequest, providerKey, notes);

    // Create 

    [Fact]
    public void Create_NullRefundAmount_IsFullRefund()
    {
        var refund = CreatePending(refundAmount: null);

        refund.Amount.Amount.Should().Be(PaymentAmount.Amount);
        refund.IsPartial.Should().BeFalse();
        refund.Status.Should().Be(RefundStatus.Pending);
    }

    [Fact]
    public void Create_PartialAmount_SetsIsPartialTrue()
    {
        var partial = new Money(3_000, "XOF");

        var refund = CreatePending(refundAmount: partial);

        refund.Amount.Amount.Should().Be(3_000);
        refund.IsPartial.Should().BeTrue();
    }

    [Fact]
    public void Create_AmountExceedsPayment_ThrowsDomainException()
    {
        var over = new Money(15_000, "XOF");

        var act = () => CreatePending(refundAmount: over);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public void Create_ZeroAmount_ThrowsDomainException()
    {
        var zero = new Money(0, "XOF");

        var act = () => CreatePending(refundAmount: zero);

        act.Should().Throw<DomainException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void Create_CurrencyMismatch_ThrowsDomainException()
    {
        var eur = new Money(100, "EUR");

        var act = () => CreatePending(refundAmount: eur);

        act.Should().Throw<DomainException>()
            .WithMessage("*currency*");
    }

    [Fact]
    public void Create_MobileMoneyProvider_EstimatesArrivalIn1Hour()
    {
        var before = DateTimeOffset.UtcNow;
        var refund = CreatePending(providerKey: "mtn_momo");

        refund.EstimatedArrival.Should()
            .BeCloseTo(before.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_StripeProvider_EstimatesArrivalIn5Days()
    {
        var before = DateTimeOffset.UtcNow;
        var refund = CreatePending(providerKey: "stripe");

        refund.EstimatedArrival.Should()
            .BeCloseTo(before.AddDays(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_PaypalProvider_EstimatesArrivalIn5Days()
    {
        var before = DateTimeOffset.UtcNow;
        var refund = CreatePending(providerKey: "paypal");

        refund.EstimatedArrival.Should()
            .BeCloseTo(before.AddDays(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_SetsCorrectIdentifiers()
    {
        var refund = CreatePending();

        refund.Id.Should().NotBeEmpty();
        refund.PaymentId.Should().Be(PaymentId);
        refund.MerchantId.Should().Be(MerchantId);
    }

    // MarkCompleted 

    [Fact]
    public void MarkCompleted_PendingRefund_SetsStatusAndReference()
    {
        var refund = CreatePending();

        refund.MarkCompleted("provider-ref-456");

        refund.Status.Should().Be(RefundStatus.Completed);
        refund.ProviderReference.Should().Be("provider-ref-456");
        refund.CompletedAt.Should().NotBeNull();
        refund.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_AlreadyCompleted_ThrowsDomainException()
    {
        var refund = CreatePending();
        refund.MarkCompleted("ref-1");

        var act = () => refund.MarkCompleted("ref-2");

        act.Should().Throw<DomainException>()
            .WithMessage("*final state*");
    }

    // MarkFailed 

    [Fact]
    public void MarkFailed_PendingRefund_SetsStatusAndRecordsReason()
    {
        var refund = CreatePending();

        refund.MarkFailed("Provider declined");

        refund.Status.Should().Be(RefundStatus.Failed);
        refund.FailedAt.Should().NotBeNull();
        refund.IsFinalState.Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_WithExistingNotes_AppendFailureReason()
    {
        var refund = CreatePending(notes: "initial note");

        refund.MarkFailed("Provider error");

        refund.Notes.Should().Contain("initial note");
        refund.Notes.Should().Contain("Provider error");
    }

    [Fact]
    public void MarkFailed_AlreadyFailed_ThrowsDomainException()
    {
        var refund = CreatePending();
        refund.MarkFailed("error 1");

        var act = () => refund.MarkFailed("error 2");

        act.Should().Throw<DomainException>()
            .WithMessage("*final state*");
    }

    [Fact]
    public void MarkFailed_AfterCompleted_ThrowsDomainException()
    {
        var refund = CreatePending();
        refund.MarkCompleted("ref-ok");

        var act = () => refund.MarkFailed("too late");

        act.Should().Throw<DomainException>();
    }
}