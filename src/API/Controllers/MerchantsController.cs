using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Merchants;
using AfriPay.Application.Merchants.Commands.CreateMerchant;
using AfriPay.Application.Merchants.Commands.UpdateWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/merchants")]
[Produces("application/json")]
public sealed class MerchantsController(IMerchantService merchants) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMerchantCommand request, CancellationToken ct)
    {
        var result = await merchants.CreateAsync(request, ct);

        return result.Match(
            onSuccess: r => StatusCode(201, new CreateMerchantResponse
            {
                MerchantId    = r.MerchantId,
                LiveApiKey    = r.LiveApiKey,
                SandboxApiKey = r.SandboxApiKey,
            }),
            onError: e => e.ToActionResult());
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await merchants.GetByIdAsync(HttpContext.GetMerchantId(), ct);
        return result.Match(
            onSuccess: dto => Ok(new MerchantResponse
            {
                MerchantId   = dto.MerchantId,
                BusinessName = dto.BusinessName,
                Email        = dto.Email,
                Country      = dto.Country,
                Status       = dto.Status,
                Plan         = dto.Plan,
                HasWebhook   = dto.HasWebhook,
                KybVerified  = dto.KybVerified,
                CreatedAt    = dto.CreatedAt,
                VerifiedAt   = dto.VerifiedAt,
            }),
            onError: e => e.ToActionResult());
    }

    [HttpPut("me/webhook")]
    [Authorize]
    public async Task<IActionResult> UpdateWebhook([FromBody] UpdateWebhookRequest request, CancellationToken ct)
    {
        var result = await merchants.UpdateWebhookAsync(new UpdateWebhookCommand(HttpContext.GetMerchantId(),request.Url, request.Secret), ct);

        return result.Match(dto => Ok(dto), e => e.ToActionResult());
    }

    [HttpPost("me/api-keys/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateApiKey([FromQuery] string keyType, CancellationToken ct)
    {
        var result = await merchants.RegenerateApiKeyAsync(HttpContext.GetMerchantId(), keyType, ct);

        return result.Match(r => Ok(r), e => e.ToActionResult());
    }
}