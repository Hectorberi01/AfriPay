using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Disputes;
using AfriPay.Application.Disputes.Commands;
using AfriPay.Application.Disputes.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/disputes")]
[Produces("application/json")]
public sealed class DisputesController(IDisputeService disputes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await disputes.ListAsync(
            new ListDisputesQuery(
                HttpContext.GetMerchantId(), status, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    [HttpGet("{disputeId:guid}")]
    public async Task<IActionResult> Get(Guid disputeId, CancellationToken ct)
    {
        var result = await disputes.GetAsync(
            new GetDisputeQuery(disputeId, HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPost("{disputeId:guid}/evidence")]
    public async Task<IActionResult> SubmitEvidence(
        Guid disputeId,
        [FromBody] SubmitEvidenceRequest request,
        CancellationToken ct)
    {
        var result = await disputes.SubmitEvidenceAsync(
            new SubmitEvidenceCommand(
                HttpContext.GetMerchantId(), disputeId,
                request.EvidenceType, request.FileName,
                request.StorageKey, request.MerchantNote, request.Description), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}

public sealed record SubmitEvidenceRequest(
    string  EvidenceType,
    string  FileName,
    string  StorageKey,
    string? MerchantNote,
    string? Description);