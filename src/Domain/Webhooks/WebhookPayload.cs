namespace AfriPay.Domain.Webhooks;

/// <summary>
/// Payload normalisé AfriPay envoyé à chaque marchand.
/// Format identique quel que soit le provider source.
/// </summary>
public sealed record WebhookPayload
{
    public required string EventId     { get; init; }   // UUID v4 unique par événement
    public required string EventType   { get; init; }   // "payment.completed", etc.
    public required string ProviderKey { get; init; }
    public required string PaymentId   { get; init; }
    public string?         RefundId    { get; init; }
    public required string Status      { get; init; }
    public long?           Amount      { get; init; }
    public string?         Currency    { get; init; }
    public Dictionary<string, string>  Metadata { get; init; } = [];
    public required DateTimeOffset OccurredAt { get; init; }
 
    /// <summary>Sérialise le payload en JSON déterministe pour la signature HMAC.</summary>
    public string ToJson() => System.Text.Json.JsonSerializer.Serialize(this,
        new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
            WriteIndented        = false,
        });
}