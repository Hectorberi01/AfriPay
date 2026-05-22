using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;
using AfriPay.Infrastructure.Providers;
using MediatR;

namespace AfriPay.Application.Payments.Commands.InitiPayment;

public sealed class InitiatePaymentHandler(IUnitOfWork uow, IPaymentOrchestrator  orchestrator) 
    : IRequestHandler<InitiatePaymentDto, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(InitiatePaymentDto dto, CancellationToken ct = default)
    {
        // Validation
        if (dto.Amount <= 0)
            return Result<PaymentDto>.Fail(
                AppError.Validation("amount", "Amount must be positive."));

        if (string.IsNullOrWhiteSpace(dto.Currency) || dto.Currency.Length != 3)
            return Result<PaymentDto>.Fail(
                AppError.Validation("currency", "Currency must be a 3-char ISO 4217 code."));

        // Idempotence — retourner le résultat existant sans appeler le provider
        var existing = await uow.Payments.GetByIdempotencyKeyAsync(dto.MerchantId, dto.IdempotencyKey, ct);

        if (existing is not null)
            return Result<PaymentDto>.Ok(PaymentDto.FromDomain(existing));

        // Charger le marchand pour le webhook URL par défaut
        var merchant = await uow.Merchants.GetByIdAsync(dto.MerchantId, ct);
        if (merchant is null)
            return Result<PaymentDto>.Fail(
                AppError.NotFound("Merchant", dto.MerchantId.ToString()));

        // Créer l'agrégat Payment
        var payment = Payment.Create(
            merchantId:     dto.MerchantId,
            idempotencyKey: dto.IdempotencyKey,
            amount:         new Money(dto.Amount, dto.Currency),
            providerKey:    dto.ProviderKey,
            customer:       new CustomerInfo(dto.PhoneNumber, dto.Email, null),
            metadata:       dto.Metadata ?? [],
            webhookUrl:     dto.WebhookUrl ?? merchant.WebhookConfig?.Url);

        // Appel provider
        var provResult = await orchestrator.InitiateAsync(
            dto.ProviderKey,
            new OrchestratorRequest(
                dto.Amount, 
                dto.Currency, 
                dto.IdempotencyKey,
                dto.PhoneNumber, 
                dto.Email, 
                dto.Metadata ?? [],
                dto.IsLive),
            ct);

        payment.RecordProviderAttempt(
            provResult.ProviderKey,
            provResult.ProviderReference,
            provResult.IsSuccess,
            provResult.ErrorCode,
            null,
            null,
            provResult.RedirectUrl
            );

        if (provResult.UssdCode is not null)
            payment.SetUssdCode(provResult.UssdCode);

        if (!provResult.IsSuccess)
            payment.MarkFailed(provResult.ErrorMessage ?? "Provider error");

        // 5. Persister + webhook outbox en une transaction
        await uow.ExecuteInTransactionAsync(async () =>
        {
            await uow.Payments.AddAsync(payment, ct);

            // Outbox webhook si configuré et paiement échoué
            if (!provResult.IsSuccess && merchant.WebhookConfig is not null)
            {
                var payload = BuildPayload(payment);
                var delivery = WebhookDelivery.Create(
                    payment.MerchantId,
                    "payment.failed",
                    payload,
                    merchant.WebhookConfig.Secret,
                    merchant.WebhookConfig.Url);
                await uow.WebhookDeliveries.AddAsync(delivery, ct);
            }
        }, ct);

        if (!provResult.IsSuccess)
            return Result<PaymentDto>.Fail(
                AppError.ProviderError(provResult.ProviderKey, provResult.ErrorMessage ?? ""));

        return Result<PaymentDto>.Ok(PaymentDto.FromDomain(payment));
    }
    
    private static WebhookPayload BuildPayload(Payment p) => new()
    {
        EventId     = Guid.NewGuid().ToString(),
        EventType   = p.Status == PaymentStatus.Completed ? "payment.completed" : "payment.failed",
        ProviderKey = p.ProviderUsed ?? p.ProviderKey,
        PaymentId   = p.Id.ToString(),
        Status      = p.Status.ToString().ToLower(),
        Amount      = p.Amount.Amount,
        Currency    = p.Amount.Currency,
        Metadata    = p.Metadata,
        OccurredAt  = DateTimeOffset.UtcNow,
    };
}