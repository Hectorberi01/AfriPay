using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Analytics;
using AfriPay.Application.Analytics.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] string         currency = "XOF",
        [FromQuery] DateTimeOffset? from    = null,
        [FromQuery] DateTimeOffset? to      = null,
        CancellationToken ct = default)
    {
        var now      = DateTimeOffset.UtcNow;
        var dateFrom = from ?? now.AddDays(-30);
        var dateTo   = to   ?? now;
 
        var result = await analytics.GetSummaryAsync(
            new GetAnalyticsSummaryQuery(
                HttpContext.GetMerchantId(),
                currency.ToUpperInvariant(),
                dateFrom, dateTo), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("payouts")]
    public async Task<IActionResult> PayoutSummary(
        [FromQuery] string currency = "XOF",
        CancellationToken ct = default)
    {
        var result = await analytics.GetPayoutSummaryAsync(
            new GetPayoutSummaryQuery(
                HttpContext.GetMerchantId(),
                currency.ToUpperInvariant()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}
