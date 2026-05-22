using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Webhook;
using AfriPay.Application.Webhook.Commands;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class WebhooksController(IWebhookService webhooks) : ControllerBase
{
    [HttpGet("v1/webhooks")]
    public async Task<IActionResult> List(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct     = default)
    {
        var result = await webhooks.ListAsync(
            new ListWebhookDeliveriesQuery(
                HttpContext.GetMerchantId(),
                Math.Clamp(page,     1, int.MaxValue),
                Math.Clamp(pageSize, 1, 100)),
            ct);

        return result.Match(
            onSuccess: paged => Ok(new PagedResponse<WebhookDeliveryResponse>
            {
                Data       = paged.Items.Select(d => new WebhookDeliveryResponse(
                    d.DeliveryId, d.EventType, d.Status,
                    d.AttemptCount, d.TargetUrl,
                    d.CreatedAt, d.DeliveredAt, d.NextRetryAt)).ToList(),
                TotalCount = paged.TotalCount,
                Page       = paged.Page,
                PageSize   = paged.PageSize,
                HasMore    = paged.HasMore,
            }),
            onError: error => error.ToActionResult());
    }

    [HttpPost("v1/webhooks/{deliveryId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid deliveryId, CancellationToken ct)
    {
        var result = await webhooks.RetryAsync(
            new RetryWebhookDeliveryCommand(deliveryId, HttpContext.GetMerchantId()), ct);

        return result.Match(
            onSuccess: dto   => Ok(new WebhookDeliveryResponse(
                dto.DeliveryId, dto.EventType, dto.Status,
                dto.AttemptCount, dto.TargetUrl,
                dto.CreatedAt, dto.DeliveredAt, dto.NextRetryAt)),
            onError: error => error.ToActionResult());
    }

    [HttpPost("webhooks/providers/{provider}")]
    public async Task<IActionResult> ReceiveProviderCallback(string provider, [FromServices] PayPalWebhookHandler paypalHandler, CancellationToken ct)
    {
        // Lire le body brut AVANT désérialisation
        Request.EnableBuffering();
        using var reader       = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody      = await reader.ReadToEndAsync(ct);
 
        _ = provider.ToLowerInvariant() switch
        {
            "paypal" => paypalHandler.HandleAsync(rawBody,  HttpContext.GetIsLive(), CancellationToken.None),
            _        => Task.CompletedTask,
        };
 
        // Toujours 200 immédiatement — traitement en fire-and-forget
        return Ok();
    }
}