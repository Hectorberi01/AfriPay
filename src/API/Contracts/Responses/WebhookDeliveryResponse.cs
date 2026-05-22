namespace AfriPay.API.Contracts.Responses;

public sealed record WebhookDeliveryResponse(
    string DeliveryId,
    string EventType,
    string Status,
    int AttemptCount,
    string TargetUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? NextRetryAt
);
