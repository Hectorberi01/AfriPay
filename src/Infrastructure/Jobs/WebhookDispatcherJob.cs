using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;

namespace AfriPay.Infrastructure.Jobs;

// WEBHOOK DISPATCHER JOB — retry des livraisons mortes
public sealed class WebhookDispatcherJob(
    IServiceScopeFactory         scopeFactory,
    ILogger<WebhookDispatcherJob> logger)
    : AfriPayJob(scopeFactory, logger, "WebhookDispatcherJob")
{
    // Tourne toutes les 30 secondes
    protected override TimeSpan GetInterval() => TimeSpan.FromSeconds(30);
 
    protected override async Task RunAsync(IServiceProvider sp, CancellationToken ct)
    {
        var uow  = sp.GetRequiredService<IUnitOfWork>();
        var http = sp.GetRequiredService<IHttpClientFactory>();
 
        var pending = await uow.WebhookDeliveries.GetDueForDeliveryAsync(ct: ct);
 
        if (!pending.Any())
        {
            logger.LogDebug("WebhookDispatcher: no pending deliveries.");
            return;
        }
 
        logger.LogInformation(
            "WebhookDispatcher: dispatching {Count} webhooks.", pending.Count);
 
        foreach (var delivery in pending)
        {
            await DispatchAsync(delivery, http, uow, ct);
        }
 
        await uow.SaveChangesAsync(ct);
    }
 
    private async Task DispatchAsync(
        WebhookDelivery    delivery,
        IHttpClientFactory http,
        IUnitOfWork        uow,
        CancellationToken  ct)
    {
        var client    = http.CreateClient("webhook");
        var startedAt = DateTimeOffset.UtcNow;
 
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.TargetUrl)
            {
                Content = new StringContent(
                    delivery.Payload,
                    System.Text.Encoding.UTF8,
                    "application/json"),
            };
 
            // Signature précalculée à la création — delivery.Signature
            request.Headers.Add("X-AfriPay-Signature",   delivery.Signature);
            request.Headers.Add("X-AfriPay-Delivery-Id", delivery.Id.ToString());
            request.Headers.Add("X-AfriPay-Event",       delivery.EventType);
 
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
 
            var response    = await client.SendAsync(request, cts.Token);
            var latencyMs   = (int)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
            var body        = await response.Content.ReadAsStringAsync(ct);
            var statusCode  = (int)response.StatusCode;
 
            // RecordAttempt gère tout : status, NextRetryAt, DeadLetter
            delivery.RecordAttempt(statusCode, body, latencyMs);
 
            if (response.IsSuccessStatusCode)
                logger.LogDebug(
                    "Webhook {Id} delivered [{Status}] → {Url}",
                    delivery.Id, statusCode, delivery.TargetUrl);
            else
                logger.LogWarning(
                    "Webhook {Id} failed [{Status}] → retry at {Next}",
                    delivery.Id, statusCode, delivery.NextRetryAt);
        }
        catch (Exception ex)
        {
            var latencyMs = (int)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
            delivery.RecordAttempt(null, ex.Message, latencyMs, failureReason: ex.Message);
            logger.LogWarning(ex, "Webhook {Id} exception.", delivery.Id);
        }
 
        uow.WebhookDeliveries.Update(delivery);
    }
}