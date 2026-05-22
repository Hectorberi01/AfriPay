using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Balance;
using AfriPay.Application.Balance.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/balance")]
[Produces("application/json")]
public sealed class BalanceController(IBalanceService balance) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBalance(
        [FromQuery] string currency = "XOF",
        CancellationToken ct = default)
    {
        var result = await balance.GetBalanceAsync(
            new GetBalanceQuery(
                HttpContext.GetMerchantId(),
                currency.ToUpperInvariant()),
            ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("all")]
    public async Task<IActionResult> ListBalances(CancellationToken ct = default)
    {
        var result = await balance.ListBalancesAsync(
            new ListBalancesQuery(HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string          currency = "XOF",
        [FromQuery] DateTimeOffset? from     = null,
        [FromQuery] DateTimeOffset? to       = null,
        [FromQuery] int             page     = 1,
        [FromQuery] int             pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await balance.GetEntriesAsync(
            new ListBalanceEntriesQuery(
                HttpContext.GetMerchantId(),
                currency.ToUpperInvariant(),
                from, to,
                Math.Clamp(page, 1, int.MaxValue),
                Math.Clamp(pageSize, 1, 200)),
            ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
}