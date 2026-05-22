using AfriPay.API.Extensions;
using AfriPay.Application.Admin;
using AfriPay.Application.Admin.Commands;
using AfriPay.Application.Admin.Queries;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes;
using AfriPay.Application.Disputes.Commands;
using AfriPay.Application.Kyb;
using AfriPay.Application.Kyb.Commands;
using AfriPay.Application.Kyb.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

/// <summary>
/// API d'administration interne AfriPay.
/// TODO : protéger avec JWT admin (rôle "afripay:admin").
/// </summary>
[ApiController]
[Route("v1/admin")]
[Produces("application/json")]
public sealed class AdminController(
    IAdminService   admin,
    IKybService     kyb,
    IDisputeService disputes) : ControllerBase
{
    // Merchants
 
    // GET /v1/admin/merchants?status=active&plan=growth
    [HttpGet("merchants")]
    public async Task<IActionResult> ListMerchants(
        [FromQuery] string? status   = null,
        [FromQuery] string? plan     = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await admin.ListMerchantsAsync(
            new ListMerchantsAdminQuery(status, plan, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/admin/merchants/{merchantId}/suspend
    [HttpPost("merchants/{merchantId:guid}/suspend")]
    public async Task<IActionResult> Suspend(
        Guid merchantId,
        [FromBody] AdminActionRequest request,
        CancellationToken ct)
    {
        var result = await admin.SuspendMerchantAsync(
            new SuspendMerchantCommand(merchantId, request.Reason ?? "Admin action", request.AdminEmail), ct);
 
        return result.Match(
            onSuccess: _ => Ok(new { message = "Merchant suspended." }),
            onError:   e => e.ToActionResult());
    }
 
    // POST /v1/admin/merchants/{merchantId}/reactivate
    [HttpPost("merchants/{merchantId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        Guid merchantId,
        [FromBody] AdminActionRequest request,
        CancellationToken ct)
    {
        var result = await admin.ReactivateMerchantAsync(
            new ReactivateMerchantCommand(merchantId, request.AdminEmail), ct);
 
        return result.Match(
            onSuccess: _ => Ok(new { message = "Merchant reactivated." }),
            onError:   e => e.ToActionResult());
    }
 
    // PUT /v1/admin/merchants/{merchantId}/plan
    [HttpPut("merchants/{merchantId:guid}/plan")]
    public async Task<IActionResult> ChangePlan(
        Guid merchantId,
        [FromBody] ChangePlanRequest request,
        CancellationToken ct)
    {
        var result = await admin.ChangePlanAsync(
            new ChangePlanCommand(merchantId, request.Plan, request.AdminEmail), ct);
 
        return result.Match(
            onSuccess: _ => Ok(new { message = $"Plan changed to {request.Plan}." }),
            onError:   e => e.ToActionResult());
    }
 
    // KYB (admin)
 
    [HttpGet("v1/admin/kyb")]
    public async Task<IActionResult> ListAdmin(
        [FromQuery] string? status   = "submitted",
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await kyb.ListAsync(
            new ListKybQuery(status, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }

    [HttpGet("kyb")]
    public async Task<IActionResult> ListKyb(
        [FromQuery] string? status   = "submitted",
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await kyb.ListAsync(
            new ListKybQuery(status, page, pageSize), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/admin/kyb/{kybId}/approve
    [HttpPost("kyb/{kybId:guid}/approve")]
    public async Task<IActionResult> ApproveKyb(
        Guid kybId, [FromBody] KybReviewRequest request, CancellationToken ct)
    {
        var result = await kyb.ApproveAsync(
            new ApproveKybCommand(kybId, request.ReviewerEmail, request.Note), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/admin/kyb/{kybId}/reject
    [HttpPost("kyb/{kybId:guid}/reject")]
    public async Task<IActionResult> RejectKyb(
        Guid kybId, [FromBody] KybReviewRequest request, CancellationToken ct)
    {
        var result = await kyb.RejectAsync(
            new RejectKybCommand(kybId, request.ReviewerEmail, request.Note ?? ""), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
    
    [HttpPost("v1/admin/kyb/{kybId:guid}/request-info")]
    public async Task<IActionResult> RequestInfo(
        Guid kybId,
        [FromBody] ReviewRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
            return AppError.Validation("note", "Request details are required.").ToActionResult();
 
        var result = await kyb.RequestAdditionalInfoAsync(
            new RequestAdditionalInfoCommand(kybId, request.ReviewerEmail, request.Note!), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    //Disputes (admin)
 
    // POST /v1/admin/disputes
    [HttpPost("disputes")]
    public async Task<IActionResult> OpenDispute(
        [FromBody] OpenDisputeAdminRequest request, CancellationToken ct)
    {
        var result = await disputes.OpenAsync(
            new OpenDisputeCommand(
                request.MerchantId, request.PaymentId,
                request.Amount, request.Currency, request.Reason,
                request.ProviderReference, request.ProviderKey, request.CustomerNote), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/admin/disputes/{disputeId}/resolve
    [HttpPost("disputes/{disputeId:guid}/resolve")]
    public async Task<IActionResult> ResolveDispute(
        Guid disputeId, [FromBody] ResolveDisputeRequest request, CancellationToken ct)
    {
        var result = await disputes.ResolveAsync(
            new ResolveDisputeCommand(disputeId, request.MerchantWon, request.Note), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}

public sealed record AdminActionRequest(string AdminEmail, string? Reason = null);
 
public sealed record ChangePlanRequest(string Plan, string AdminEmail);
 
public sealed record KybReviewRequest(string ReviewerEmail, string? Note);
 
public sealed record OpenDisputeAdminRequest(
    Guid    MerchantId,
    Guid    PaymentId,
    long    Amount,
    string  Currency,
    string  Reason,
    string? ProviderReference,
    string? ProviderKey,
    string? CustomerNote);
 
public sealed record ResolveDisputeRequest(bool MerchantWon, string? Note);