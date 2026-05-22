using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Refunds;
using AfriPay.Application.Refunds.Commands;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Refunds;
using AfriPay.Domain.Repositories;
using NSubstitute;

namespace AfriPay.Tests.Application.Refunds;

public sealed class CreateRefundHandlerTests
{
    private readonly IUnitOfWork          _uow      = Substitute.For<IUnitOfWork>();
    private readonly IPaymentRepository   _payments = Substitute.For<IPaymentRepository>();
    private readonly IRefundRepository    _refunds  = Substitute.For<IRefundRepository>();
    private readonly CreateRefundHandler  _handler;
    private static readonly Guid         MerchantId = Guid.NewGuid();

    public CreateRefundHandlerTests()
    {
        _uow.Payments.Returns(_payments);
        _uow.Refunds.Returns(_refunds);
        _handler = new CreateRefundHandler(_uow);
    }

    private static Payment CreateCompletedPayment(Guid? merchantId = null)
    {
        var p = Payment.Create(
            merchantId ?? MerchantId, "key-001",
            new Money(10_000, "XOF"), "mtn_momo", null, null, null);
        p.MarkCompleted("ref-123", "mtn_momo");
        return p;
    }

    private static CreateRefundCommand FullRefundCmd(Guid paymentId) => new(
        PaymentId:      paymentId,
        MerchantId:     MerchantId,
        IdempotencyKey: "ref-idem-001",
        Amount:         null,
        Reason:         "customer_request",
        Notes:          null);

    // ── Payment lookup ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PaymentNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _payments.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var result = await _handler.HandleAsync(FullRefundCmd(id), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(404);
        result.Error.Code.Should().Be("PAYMENT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_PaymentBelongsToOtherMerchant_ReturnsUnauthorized()
    {
        var payment = CreateCompletedPayment(Guid.NewGuid()); // different merchant
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.HandleAsync(FullRefundCmd(payment.Id), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(401);
    }

    // ── Payment status ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PaymentNotCompleted_ReturnsConflictError()
    {
        var payment = Payment.Create(
            MerchantId, "key-001", new Money(10_000, "XOF"), "mtn_momo", null, null, null); // Pending
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.HandleAsync(FullRefundCmd(payment.Id), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(409);
    }

    // ── Amount validation ────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_RefundAmountExceedsPayment_ReturnsValidationError()
    {
        var payment = CreateCompletedPayment(); // 10_000 XOF
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(0L);

        var cmd    = FullRefundCmd(payment.Id) with { Amount = 15_000 };
        var result = await _handler.HandleAsync(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task HandleAsync_TotalRefundsWouldExceedPaymentAmount_ReturnsConflictError()
    {
        var payment = CreateCompletedPayment(); // 10_000 XOF
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(8_000L);

        // 8_000 already + 5_000 requested = 13_000 > 10_000
        var cmd    = FullRefundCmd(payment.Id) with { Amount = 5_000 };
        var result = await _handler.HandleAsync(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(409);
    }

    // ── Reason validation ────────────────────────────────────────

    [Theory]
    [InlineData("unknown_reason")]
    [InlineData("")]
    [InlineData("refund_please")]
    public async Task HandleAsync_InvalidReason_ReturnsValidationError(string reason)
    {
        var payment = CreateCompletedPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(0L);

        var cmd    = FullRefundCmd(payment.Id) with { Reason = reason };
        var result = await _handler.HandleAsync(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    // ── Success ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidFullRefund_ReturnsRefundDto()
    {
        var payment = CreateCompletedPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(0L);

        var result = await _handler.HandleAsync(FullRefundCmd(payment.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentId.Should().Be(payment.Id.ToString());
        result.Value.IsPartial.Should().BeFalse();
        result.Value.Amount.Should().Be(10_000);
        await _refunds.Received(1).AddAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidPartialRefund_IsPartialTrueAndCorrectAmount()
    {
        var payment = CreateCompletedPayment(); // 10_000 XOF
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(0L);

        var cmd    = FullRefundCmd(payment.Id) with { Amount = 3_000 };
        var result = await _handler.HandleAsync(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPartial.Should().BeTrue();
        result.Value.Amount.Should().Be(3_000);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("fraudulent")]
    [InlineData("customer_request")]
    [InlineData("other")]
    public async Task HandleAsync_AllValidReasons_Succeed(string reason)
    {
        var payment = CreateCompletedPayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetTotalRefundedAmountAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(0L);

        var cmd    = FullRefundCmd(payment.Id) with { Reason = reason };
        var result = await _handler.HandleAsync(cmd, default);

        result.IsSuccess.Should().BeTrue();
    }
}