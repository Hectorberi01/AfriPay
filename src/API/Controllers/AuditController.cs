using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Audit;
using AfriPay.Application.Audit.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/audit")]
[Produces("application/json")]
public sealed class AuditController(IAuditService audit) : ControllerBase
{
    // GET /v1/audit?action=PaymentCompleted&from=2026-01-01
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string?         action   = null,
        [FromQuery] DateTimeOffset? from     = null,
        [FromQuery] DateTimeOffset? to       = null,
        [FromQuery] int             page     = 1,
        [FromQuery] int             pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await audit.ListAsync(
            new ListAuditEntriesQuery(
                HttpContext.GetMerchantId(),
                action, from, to, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // GET /v1/audit/payments/{paymentId}
    [HttpGet("payments/{paymentId}")]
    public async Task<IActionResult> GetPaymentHistory(
        string paymentId, CancellationToken ct)
    {
        var result = await audit.GetEntityHistoryAsync(
            new GetEntityAuditQuery("Payment", paymentId), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
}