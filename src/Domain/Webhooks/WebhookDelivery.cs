namespace AfriPay.Domain.Webhooks;

/// <summary>
/// Agrégat WebhookDelivery — implémente l'outbox pattern.
///
/// Invariants métier :
///   - Créé en même transaction que l'événement source (atomicité outbox).
///   - Une livraison Delivered ou DeadLetter ne peut plus changer.
///   - Chaque tentative est tracée dans _attempts.
///   - Le retry schedule est calculé par le domaine, pas par l'infra.
///   - La signature HMAC est calculée une fois à la création et ne change plus.
///
/// Garantie de livraison : at-least-once.
/// Le marchand DOIT être idempotent sur event_id pour éviter les doublons.
/// </summary>
public sealed class WebhookDelivery
{
    //Identité
 
    public Guid   Id          { get; private set; }
    public Guid   MerchantId  { get; private set; }
    
    /// <summary>Type d'événement ("payment.completed", etc.).</summary>
    public string EventType   { get; private set; } = default!;
 
    /// <summary>Payload JSON sérialisé. Immuable après création.</summary>
    public string Payload     { get; private set; } = default!;
 
    /// <summary>
    /// Signature HMAC-SHA256 du payload avec le secret du marchand.
    /// Format : "sha256=<hex>"
    /// </summary>
    public string Signature   { get; private set; } = default!;
 
    /// <summary>URL de livraison au moment de la création. Immuable.</summary>
    public string TargetUrl   { get; private set; } = default!;
 
    // Statut
    public DeliveryStatus Status        { get; private set; }
    public int            AttemptCount  { get; private set; }
    public DateTimeOffset? NextRetryAt  { get; private set; }
    public DateTimeOffset? DeliveredAt  { get; private set; }
    public DateTimeOffset  CreatedAt    { get; private set; }
 
    // Tentatives
 
    private readonly List<DeliveryAttempt> _attempts = [];
    public IReadOnlyList<DeliveryAttempt> Attempts => _attempts.AsReadOnly();
 
    // Computed 
 
    public bool IsTerminal   => Status is DeliveryStatus.Delivered or DeliveryStatus.DeadLetter;
    public bool IsDue        => !IsTerminal && (NextRetryAt is null || NextRetryAt <= DateTimeOffset.UtcNow);
 
    private WebhookDelivery() { }
    
    /// <summary>
    /// Crée une nouvelle livraison webhook prête à être envoyée.
    /// Signe le payload avec le secret HMAC du marchand.
    /// </summary>
    public static WebhookDelivery Create(
        Guid           merchantId,
        string         eventType,
        WebhookPayload payload,
        string         merchantSecret,
        string         targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
            throw new WebhookDomainException("TargetUrl is required.");
 
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new WebhookDomainException($"TargetUrl '{targetUrl}' must be a valid HTTPS URL.");
 
        var json      = payload.ToJson();
        var signature = ComputeSignature(json, merchantSecret);
 
        return new WebhookDelivery
        {
            Id          = Guid.NewGuid(),
            MerchantId  = merchantId,
            EventType   = eventType,
            Payload     = json,
            Signature   = signature,
            TargetUrl   = targetUrl,
            Status      = DeliveryStatus.Pending,
            AttemptCount = 0,
            CreatedAt   = DateTimeOffset.UtcNow,
            NextRetryAt = null,   // livrable immédiatement
        };
    }

    /// <summary>
    /// Enregistre le résultat d'une tentative de livraison HTTP.
    /// Calcule automatiquement le prochain retry ou passe en dead-letter.
    /// </summary>
    public void RecordAttempt(
        int? httpStatusCode,
        string? responseBody,
        int? latencyMs,
        string? failureReason = null)
    {
        if (IsTerminal)
            throw new WebhookDomainException(
                $"Cannot record attempt on a {Status} delivery.");
 
        var success = httpStatusCode is >= 200 and < 300;
        AttemptCount++;
 
        _attempts.Add(DeliveryAttempt.Create(Id, AttemptCount, httpStatusCode,
            responseBody, latencyMs, success, failureReason));
 
        if (success)
        {
            Status      = DeliveryStatus.Delivered;
            DeliveredAt = DateTimeOffset.UtcNow;
            NextRetryAt = null;
        }
        else if (AttemptCount >= RetrySchedule.MaxAttempts)
        {
            Status      = DeliveryStatus.DeadLetter;
            NextRetryAt = null;
        }
        else
        {
            var delay   = RetrySchedule.NextDelay(AttemptCount);
            Status      = DeliveryStatus.Retrying;
            NextRetryAt = DateTimeOffset.UtcNow.Add(delay ?? TimeSpan.FromMinutes(5));
        }
    }
    
    /// <summary>
    /// Réinitialise une livraison dead-letter pour un retry manuel.
    /// Uniquement via une action explicite de l'équipe support.
    /// </summary>
    public void ResetForManualRetry()
    {
        if (Status != DeliveryStatus.DeadLetter)
            throw new WebhookDomainException("Only dead-letter deliveries can be manually retried.");
 
        Status       = DeliveryStatus.Pending;
        AttemptCount = 0;
        NextRetryAt  = null;
    }
    
    /// <summary>
    /// Calcule la signature HMAC-SHA256 du payload JSON.
    /// Format retourné : "sha256=<lowercase hex>"
    /// </summary>
    public static string ComputeSignature(string payload, string secret)
    {
        var key  = System.Text.Encoding.UTF8.GetBytes(secret);
        var body = System.Text.Encoding.UTF8.GetBytes(payload);
 
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        var hash = hmac.ComputeHash(body);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
 
    /// <summary>
    /// Vérifie une signature HMAC en temps constant (protection contre timing attacks).
    /// </summary>
    public static bool VerifySignature(string payload, string signature, string secret)
    {
        var expected = ComputeSignature(payload, secret);
        var sigBytes = System.Text.Encoding.UTF8.GetBytes(signature);
        var expBytes = System.Text.Encoding.UTF8.GetBytes(expected);
 
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(sigBytes, expBytes);
    }
}

public sealed class WebhookDomainException(string message) : Exception(message);