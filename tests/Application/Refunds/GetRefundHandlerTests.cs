using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Refunds;
using AfriPay.Application.Refunds.Queries;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Refunds;
using AfriPay.Domain.Repositories;
using NSubstitute;

namespace AfriPay.Tests.Application.Refunds;

public sealed class GetRefundHandlerTests
{
    private readonly IUnitOfWork        _uow     = Substitute.For<IUnitOfWork>();
    private readonly IRefundRepository  _refunds = Substitute.For<IRefundRepository>();
    private readonly GetRefundHandler   _handler;
    private static readonly Guid       MerchantId = Guid.NewGuid();
    private static readonly Guid       PaymentId  = Guid.NewGuid();

    public GetRefundHandlerTests()
    {
        _uow.Refunds.Returns(_refunds);
        _handler = new GetRefundHandler(_uow);
    }

    private static Refund CreateRefund(Guid? merchantId = null) =>
        Refund.Create(
            paymentId:     PaymentId,
            merchantId:    merchantId ?? MerchantId,
            paymentAmount: new Money(10_000, "XOF"),
            refundAmount:  null,
            reason:        RefundReason.CustomerRequest,
            providerKey:   "mtn_momo",
            notes:         null);

    // ── Found ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ExistingRefund_ReturnsDto()
    {
        var refund = CreateRefund();
        _refunds.GetByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);

        var result = await _handler.HandleAsync(new GetRefundQuery(refund.Id, MerchantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundId.Should().Be(refund.Id.ToString());
        result.Value.PaymentId.Should().Be(PaymentId.ToString());
    }

    // ── Not found ────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_RefundNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _refunds.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        var result = await _handler.HandleAsync(new GetRefundQuery(id, MerchantId), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(404);
        result.Error.Code.Should().Be("REFUND_NOT_FOUND");
    }

    // ── Unauthorized ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_RefundBelongsToOtherMerchant_ReturnsUnauthorized()
    {
        var refund = CreateRefund(Guid.NewGuid()); // different merchant
        _refunds.GetByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);

        var result = await _handler.HandleAsync(new GetRefundQuery(refund.Id, MerchantId), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(401);
    }
}