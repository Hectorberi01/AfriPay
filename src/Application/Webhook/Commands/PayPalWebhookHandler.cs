using System.Text.Json;
using System.Text.Json.Serialization;
using AfriPay.Infrastructure.Providers.Paypal;

namespace AfriPay.Application.Webhook.Commands;


internal sealed record PayPalWebhookEvent(
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("event_type")]  string EventType,
    [property: JsonPropertyName("resource")]    JsonElement Resource
);
 
internal sealed record PayPalOrderResource(
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("status")]      string Status,
    [property: JsonPropertyName("intent")]      string? Intent
);


/// <summary>
/// Traite les webhooks entrants de PayPal.
///
/// Flux complet PayPal :
///   1. AfriPay crée un Order → retourne approve_url
///   2. Marchand redirige le payeur vers approve_url
///   3. Payeur approuve sur paypal.com
///   4. PayPal envoie CHECKOUT.ORDER.APPROVED → ce handler
///   5. Ce handler appelle CaptureOrder → COMPLETED
///   6. PayPal envoie PAYMENT.CAPTURE.COMPLETED → confirmer en base
/// </summary>
public class PayPalWebhookHandler (PayPalClient client, ILogger<PayPalWebhookHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    
    public async Task<WebhookHandlerResult> HandleAsync(string rawBody, bool isLive, CancellationToken ct = default)
    {
        PayPalWebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<PayPalWebhookEvent>(rawBody, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "PayPal webhook body is not valid JSON");
            return WebhookHandlerResult.Ignored("Invalid JSON");
        }
 
        if (evt is null)
            return WebhookHandlerResult.Ignored("Empty event");
 
        logger.LogInformation(
            "PayPal webhook received: event_type={EventType} id={EventId}",
            evt.EventType, evt.Id);
 
        return evt.EventType switch
        {
            // Payeur a approuvé — capturer immédiatement
            "CHECKOUT.ORDER.APPROVED"
                => await HandleOrderApprovedAsync(evt, isLive, ct),
 
            // Capture confirmée — le payment peut être marqué Completed
            "PAYMENT.CAPTURE.COMPLETED"
                => HandleCaptureCompleted(evt),
 
            // Capture refusée
            "PAYMENT.CAPTURE.DENIED"
                => HandleCaptureDenied(evt),
 
            // Ignorer les autres événements
            _ => WebhookHandlerResult.Ignored($"Unhandled event type: {evt.EventType}"),
        };
    }
    
    private async Task<WebhookHandlerResult> HandleOrderApprovedAsync(PayPalWebhookEvent evt, bool isLive, CancellationToken ct)
    {
        var orderId = evt.Resource.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(orderId))
            return WebhookHandlerResult.Ignored("Missing order id in resource");
 
        logger.LogInformation("PayPal Order approved — capturing: orderId={OrderId}", orderId);
 
        try
        {
            var token    = await client.GetAccessTokenAsync(isLive, ct);
            var captured = await client.CaptureOrderAsync(orderId, token, isLive, ct);
 
            logger.LogInformation(
                "PayPal Order captured: orderId={OrderId} status={Status}",
                orderId, captured.Status);
 
            return WebhookHandlerResult.Captured(orderId, captured.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "PayPal capture failed for orderId={OrderId}", orderId);
            return WebhookHandlerResult.Failed(orderId, ex.Message);
        }
    }
    
    private WebhookHandlerResult HandleCaptureCompleted(PayPalWebhookEvent evt)
    {
        var captureId = evt.Resource.TryGetProperty("id", out var idProp)
            ? idProp.GetString()
            : null;
 
        var orderId = evt.Resource.TryGetProperty("supplementary_data", out var supp)
                      && supp.TryGetProperty("related_ids", out var related)
                      && related.TryGetProperty("order_id", out var oid)
            ? oid.GetString()
            : null;
 
        logger.LogInformation(
            "PayPal capture completed: captureId={CaptureId} orderId={OrderId}",
            captureId, orderId);
 
        return WebhookHandlerResult.Completed(orderId ?? captureId ?? "unknown");
    }
    
    private WebhookHandlerResult HandleCaptureDenied(PayPalWebhookEvent evt)
    {
        var orderId = evt.Resource.TryGetProperty("id", out var idProp)
            ? idProp.GetString() ?? "unknown"
            : "unknown";
 
        logger.LogWarning("PayPal capture denied: orderId={OrderId}", orderId);
        return WebhookHandlerResult.Failed(orderId, "Capture denied by PayPal");
    }
}

public sealed record WebhookHandlerResult(
    WebhookHandlerStatus Status,
    string?              ProviderReference,
    string?              Message)
{
    public static WebhookHandlerResult Ignored(string reason)
        => new(WebhookHandlerStatus.Ignored, null, reason);
 
    public static WebhookHandlerResult Captured(string orderId, string status)
        => new(WebhookHandlerStatus.Captured, orderId, status);
 
    public static WebhookHandlerResult Completed(string reference)
        => new(WebhookHandlerStatus.Completed, reference, null);
 
    public static WebhookHandlerResult Failed(string reference, string error)
        => new(WebhookHandlerStatus.Failed, reference, error);
}
 
public enum WebhookHandlerStatus
{
    Ignored,
    Captured,
    Completed,
    Failed,
}