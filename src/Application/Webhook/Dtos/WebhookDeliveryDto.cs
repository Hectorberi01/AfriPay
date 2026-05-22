using AfriPay.Domain.Webhooks;

namespace AfriPay.Application.Webhook.Dtos;

public sealed record WebhookDeliveryDto(
    string          DeliveryId,
    string          EventType,
    string          Status,
    int             AttemptCount,
    string          TargetUrl,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? NextRetryAt)
{
    public static WebhookDeliveryDto FromDomain(WebhookDelivery w) => new(
        w.Id.ToString(),
        w.EventType,
        w.Status.ToString().ToLower(),
        w.AttemptCount,
        w.TargetUrl,
        w.CreatedAt,
        w.DeliveredAt,
        w.NextRetryAt);
}