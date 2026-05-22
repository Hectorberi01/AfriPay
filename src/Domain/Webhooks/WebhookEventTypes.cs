namespace AfriPay.Domain.Webhooks;

/// <summary>Types d'événements AfriPay envoyés aux marchands.</summary>
public static class WebhookEventTypes
{
    public const string PaymentCompleted   = "payment.completed";
    public const string PaymentFailed      = "payment.failed";
    public const string PaymentCancelled   = "payment.cancelled";
    public const string PaymentExpired     = "payment.expired";
    public const string RefundCompleted    = "refund.completed";
    public const string RefundFailed       = "refund.failed";
    public const string ProviderDegraded   = "provider.degraded";
    public const string ProviderRecovered  = "provider.recovered";
}