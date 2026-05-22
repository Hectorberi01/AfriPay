using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Payments.Queries.GetPayment;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Repositories;
using NSubstitute;

namespace AfriPay.Tests.Application.Payments;

public sealed class GetPaymentHandlerTests
{
    private readonly IUnitOfWork           _uow      = Substitute.For<IUnitOfWork>();
    private readonly IPaymentRepository    _payments = Substitute.For<IPaymentRepository>();
    private readonly GetPaymentHandler     _handler;
    private static readonly Guid          MerchantId = Guid.NewGuid();

    public GetPaymentHandlerTests()
    {
        _uow.Payments.Returns(_payments);
        _handler = new GetPaymentHandler(_uow);
    }

    private static Payment CreatePayment(Guid? merchantId = null) =>
        Payment.Create(merchantId ?? MerchantId, "key-001", new Money(5000, "XOF"), "mtn_momo", null, null, null);

    // ── Found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingPayment_ReturnsDto()
    {
        var payment = CreatePayment();
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new GetPaymentQuery(payment.Id, MerchantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(payment.Id);
        result.Value.Status.Should().Be("pending");
        result.Value.Amount.Should().Be(5000);
        result.Value.Currency.Should().Be("XOF");
    }

    // ── Not found ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _payments.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var result = await _handler.Handle(new GetPaymentQuery(id, MerchantId), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(404);
        result.Error.Code.Should().Be("PAYMENT_NOT_FOUND");
    }

    // ── Unauthorized ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentBelongsToOtherMerchant_ReturnsUnauthorized()
    {
        var payment = CreatePayment(Guid.NewGuid()); // different merchant
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await _handler.Handle(new GetPaymentQuery(payment.Id, MerchantId), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(401);
    }
}