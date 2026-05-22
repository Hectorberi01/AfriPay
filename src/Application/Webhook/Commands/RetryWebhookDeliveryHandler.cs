using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Webhook.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;

namespace AfriPay.Application.Webhook.Commands;

//public sealed record RetryWebhookDeliveryCommand(Guid DeliveryId, Guid MerchantId);

public sealed class RetryWebhookDeliveryHandler(IUnitOfWork uow)
{
    public async Task<Result<WebhookDeliveryDto>> HandleAsync(
        RetryWebhookDeliveryCommand cmd, CancellationToken ct = default)
    {
        var delivery = await uow.WebhookDeliveries.GetWithAttemptsAsync(cmd.DeliveryId, ct);
 
        if (delivery is null)
            return Result<WebhookDeliveryDto>.Fail(
                AppError.NotFound("WebhookDelivery", cmd.DeliveryId.ToString()));
 
        if (delivery.MerchantId != cmd.MerchantId)
            return Result<WebhookDeliveryDto>.Fail(AppError.Unauthorized());
 
        if (delivery.Status != DeliveryStatus.DeadLetter)
            return Result<WebhookDeliveryDto>.Fail(
                AppError.Conflict(
                    $"Only dead_letter deliveries can be retried. Current status: {delivery.Status}."));
 
        delivery.ResetForManualRetry();
        uow.WebhookDeliveries.Update(delivery);
        await uow.SaveChangesAsync(ct);
 
        return Result<WebhookDeliveryDto>.Ok(WebhookDeliveryDto.FromDomain(delivery));
    }
}