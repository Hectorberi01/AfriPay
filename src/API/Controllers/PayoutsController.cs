using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Payouts;
using AfriPay.Application.Payouts.Commands;
using AfriPay.Application.Payouts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/payouts")]
[Produces("application/json")]
public sealed class PayoutsController(IPayoutService payouts) : ControllerBase
{
    // POST /v1/payouts
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePayoutRequest request,
        CancellationToken ct)
    {
        var result = await payouts.CreateAsync(new CreatePayoutCommand(
            HttpContext.GetMerchantId(),
            request.Amount,
            request.Currency,
            request.Method,
            request.ProviderKey,
            request.PhoneNumber,
            request.Iban,
            request.Bic,
            request.AccountHolder,
            request.BankName,
            request.Notes), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    // GET /v1/payouts/{payoutId}
    [HttpGet("{payoutId:guid}")]
    public async Task<IActionResult> Get(Guid payoutId, CancellationToken ct)
    {
        var result = await payouts.GetAsync(
            new GetPayoutQuery(payoutId, HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // GET /v1/payouts?status=pending&page=1
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await payouts.ListAsync(
            new ListPayoutsQuery(
                HttpContext.GetMerchantId(),
                status, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // DELETE /v1/payouts/{payoutId}
    [HttpDelete("{payoutId:guid}")]
    public async Task<IActionResult> Cancel(
        Guid payoutId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await payouts.CancelAsync(
            new CancelPayoutCommand(
                payoutId, HttpContext.GetMerchantId(), reason), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}

public sealed record CreatePayoutRequest(
    long    Amount,
    string  Currency,
    string  Method,         // "mobile_money" | "bank_transfer"
    string? ProviderKey,    // "mtn_momo" | "wave"…
    string? PhoneNumber,
    string? Iban,
    string? Bic,
    string? AccountHolder,
    string? BankName,
    string? Notes);