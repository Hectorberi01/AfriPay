namespace AfriPay.Domain.Webhooks;

/// <summary>
/// Enregistrement de chaque tentative de livraison HTTP.
/// </summary>
public sealed class DeliveryAttempt
{
    public Guid    Id             { get; private set; }
    public Guid    DeliveryId     { get; private set; }
    public int     AttemptNumber  { get; private set; }
    public int?    HttpStatusCode { get; private set; }
    public string? ResponseBody   { get; private set; }
    public int?    LatencyMs      { get; private set; }
    public bool    Success        { get; private set; }
    public string? FailureReason  { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
 
    private DeliveryAttempt() { }
 
    internal static DeliveryAttempt Create(
        Guid    deliveryId,
        int     attemptNumber,
        int?    httpStatusCode,
        string? responseBody,
        int?    latencyMs,
        bool    success,
        string? failureReason) => new()
    {
        Id             = Guid.NewGuid(),
        DeliveryId     = deliveryId,
        AttemptNumber  = attemptNumber,
        HttpStatusCode = httpStatusCode,
        ResponseBody   = responseBody?[..Math.Min(500, responseBody.Length)],  // tronqué
        LatencyMs      = latencyMs,
        Success        = success,
        FailureReason  = failureReason,
        AttemptedAt    = DateTimeOffset.UtcNow,
    };
}