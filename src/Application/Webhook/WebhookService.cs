using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Webhook.Commands;
using AfriPay.Application.Webhook.Dtos;
using AfriPay.Application.Webhook.Queries;

namespace AfriPay.Application.Webhook;

public interface IWebhookService
{
    Task<Result<PagedResult<WebhookDeliveryDto>>> ListAsync(ListWebhookDeliveriesQuery query, CancellationToken ct = default);
    Task<Result<WebhookDeliveryDto>> RetryAsync(RetryWebhookDeliveryCommand cmd, CancellationToken ct = default);
}

public sealed class WebhookService(ListWebhookDeliveriesHandler listHandler, RetryWebhookDeliveryHandler  retryHandler) : IWebhookService
{
    public Task<Result<PagedResult<WebhookDeliveryDto>>> ListAsync(
        ListWebhookDeliveriesQuery query, CancellationToken ct = default)
        => listHandler.HandleAsync(query, ct);
 
    public Task<Result<WebhookDeliveryDto>> RetryAsync(
        RetryWebhookDeliveryCommand cmd, CancellationToken ct = default)
        => retryHandler.HandleAsync(cmd, ct);
}