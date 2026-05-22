using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Subscriptions;
using AfriPay.Application.Subscriptions.Commands;
using AfriPay.Application.Subscriptions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/subscriptions")]
[Produces("application/json")]
public sealed class SubscriptionsController(ISubscriptionService subscriptions) : ControllerBase
{
 
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreatePlanRequest request,
        CancellationToken ct)
    {
        var result = await subscriptions.CreatePlanAsync(new CreatePlanCommand(
            HttpContext.GetMerchantId(),
            request.Name, request.Amount, request.Currency,
            request.Interval, request.ProviderKey,
            request.IntervalCount, request.TrialDays, request.Description), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans(CancellationToken ct)
    {
        var result = await subscriptions.ListPlansAsync(
            new ListPlansQuery(HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // Subscriptions 
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await subscriptions.CreateAsync(new CreateSubscriptionCommand(
            HttpContext.GetMerchantId(),
            request.PlanId,
            request.CustomerPhone,
            request.CustomerEmail,
            request.CustomerName), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("{subscriptionId:guid}")]
    public async Task<IActionResult> Get(Guid subscriptionId, CancellationToken ct)
    {
        var result = await subscriptions.GetAsync(
            new GetSubscriptionQuery(HttpContext.GetMerchantId(), subscriptionId), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await subscriptions.ListAsync(
            new ListSubscriptionsQuery(
                HttpContext.GetMerchantId(), status, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    [HttpDelete("{subscriptionId:guid}")]
    public async Task<IActionResult> Cancel(
        Guid subscriptionId,
        [FromQuery] bool immediately = false,
        CancellationToken ct = default)
    {
        var result = await subscriptions.CancelAsync(
            new CancelSubscriptionCommand(
                HttpContext.GetMerchantId(), subscriptionId, immediately), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}
 
// Request contracts 
 
public sealed record CreatePlanRequest(
    string  Name,
    long    Amount,
    string  Currency,
    string  Interval,       // "daily" | "weekly" | "monthly" | "quarterly" | "yearly"
    string  ProviderKey,
    int     IntervalCount = 1,
    int     TrialDays     = 0,
    string? Description   = null);
 
public sealed record CreateSubscriptionRequest(
    Guid    PlanId,
    string  CustomerPhone,
    string? CustomerEmail,
    string? CustomerName);