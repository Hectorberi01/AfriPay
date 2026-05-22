using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Commands.InitiPayment;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;
using AfriPay.Infrastructure.Providers;
using NSubstitute;

namespace AfriPay.Tests.Application.Payments;

public sealed class InitiatePaymentHandlerTests
{
    private readonly IUnitOfWork              _uow         = Substitute.For<IUnitOfWork>();
    private readonly IPaymentRepository       _payments    = Substitute.For<IPaymentRepository>();
    private readonly IMerchantRepository      _merchants   = Substitute.For<IMerchantRepository>();
    private readonly IWebhookDeliveryRepository _webhooks  = Substitute.For<IWebhookDeliveryRepository>();
    private readonly IPaymentOrchestrator     _orchestrator = Substitute.For<IPaymentOrchestrator>();
    private readonly InitiatePaymentHandler   _handler;
    private static readonly Guid              MerchantId   = Guid.NewGuid();

    public InitiatePaymentHandlerTests()
    {
        _uow.Payments.Returns(_payments);
        _uow.Merchants.Returns(_merchants);
        _uow.WebhookDeliveries.Returns(_webhooks);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<Func<Task>>(0)());
        _handler = new InitiatePaymentHandler(_uow, _orchestrator);
    }

    private static (Merchant Merchant, string LiveKey, string SandboxKey) CreateMerchant() =>
        Merchant.Create("AfriStore", "store@afri.io", "SN");

    private static InitiatePaymentDto ValidDto() => new(
        MerchantId:     MerchantId,
        IdempotencyKey: "idem-001",
        Amount:         5000,
        Currency:       "XOF",
        ProviderKey:    "mtn_momo",
        PhoneNumber:    "+22961234567",
        Email:          null,
        Metadata:       null,
        WebhookUrl:     null,
        IsLive:         false);

    private static OrchestratorResult SuccessResult() => new(
        ProviderKey:       "mtn_momo",
        ProviderReference: "ref-abc123",
        IsSuccess:         true,
        ErrorCode:         null,
        ErrorMessage:      null,
        UssdCode:          "*126#",
        RedirectUrl:       null);

    private static OrchestratorResult FailureResult() => new(
        ProviderKey:       "mtn_momo",
        ProviderReference: null,
        IsSuccess:         false,
        ErrorCode:         "INSUFFICIENT_FUNDS",
        ErrorMessage:      "Insufficient funds",
        UssdCode:          null,
        RedirectUrl:       null);

    // ── Input validation ─────────────────────────────────────────

    [Fact]
    public async Task Handle_AmountNotPositive_ReturnsValidationError()
    {
        var dto = ValidDto() with { Amount = 0 };

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task Handle_InvalidCurrency_ReturnsValidationError()
    {
        var dto = ValidDto() with { Currency = "XO" };

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    // ── Idempotence ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingIdempotencyKey_ReturnsExistingPaymentWithoutCallingProvider()
    {
        var dto      = ValidDto();
        var existing = Payment.Create(MerchantId, dto.IdempotencyKey, new Money(5000, "XOF"), "mtn_momo", null, null, null);
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        await _orchestrator.DidNotReceive()
            .InitiateAsync(Arg.Any<string>(), Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>());
    }

    // ── Merchant lookup ──────────────────────────────────────────

    [Fact]
    public async Task Handle_MerchantNotFound_ReturnsNotFoundError()
    {
        var dto = ValidDto();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>())
            .Returns((Merchant?)null);

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(404);
    }

    // ── Provider success ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ProviderSuccess_ReturnsPendingPaymentDto()
    {
        var dto               = ValidDto();
        var (merchant, _, _)  = CreateMerchant();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        _orchestrator.InitiateAsync(dto.ProviderKey, Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>())
            .Returns(SuccessResult());

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("pending");
        result.Value.Amount.Should().Be(5000);
        result.Value.Currency.Should().Be("XOF");
    }

    [Fact]
    public async Task Handle_ProviderSuccess_PersistsPayment()
    {
        var dto               = ValidDto();
        var (merchant, _, _)  = CreateMerchant();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        _orchestrator.InitiateAsync(dto.ProviderKey, Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>())
            .Returns(SuccessResult());

        await _handler.Handle(dto, default);

        await _payments.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderSuccessWithUssdCode_SetsUssdCodeOnDto()
    {
        var dto               = ValidDto();
        var (merchant, _, _)  = CreateMerchant();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        _orchestrator.InitiateAsync(dto.ProviderKey, Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>())
            .Returns(SuccessResult()); // UssdCode = "*126#"

        var result = await _handler.Handle(dto, default);

        result.Value!.UssdCode.Should().Be("*126#");
    }

    // ── Provider failure ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ProviderFailure_ReturnsProviderError()
    {
        var dto               = ValidDto();
        var (merchant, _, _)  = CreateMerchant();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        _orchestrator.InitiateAsync(dto.ProviderKey, Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>())
            .Returns(FailureResult());

        var result = await _handler.Handle(dto, default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(502);
        result.Error.Code.Should().Be("PROVIDER_ERROR");
    }

    [Fact]
    public async Task Handle_ProviderFailure_StillPersistsPaymentAsFailedState()
    {
        var dto               = ValidDto();
        var (merchant, _, _)  = CreateMerchant();
        _payments.GetByIdempotencyKeyAsync(MerchantId, dto.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _merchants.GetByIdAsync(MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        _orchestrator.InitiateAsync(dto.ProviderKey, Arg.Any<OrchestratorRequest>(), Arg.Any<CancellationToken>())
            .Returns(FailureResult());

        await _handler.Handle(dto, default);

        await _payments.Received(1).AddAsync(
            Arg.Is<Payment>(p => p.Status == PaymentStatus.Failed),
            Arg.Any<CancellationToken>());
    }
}