using AfriPay.API.Extensions;
using AfriPay.Application.Employees;
using AfriPay.Application.Employees.Commands;
using AfriPay.Application.Employees.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/team")]
[Authorize]
[Produces("application/json")]
public sealed class TeamController(IEmployeeService employees) : ControllerBase
{
    // GET /v1/team
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var result = await employees.ListAsync(
            new ListEmployeesQuery(GetMerchantId(), role), ct);
 
        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }
 
    // GET /v1/team/{employeeId}
    [HttpGet("{employeeId:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken ct)
    {
        var result = await employees.GetAsync(
            new GetEmployeeQuery(GetMerchantId(), employeeId), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/team
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct)
    {
        var result = await employees.CreateAsync(new CreateEmployeeCommand(
            MerchantId: GetMerchantId(),
            ActorId:    GetActorId(),
            ActorRole:  GetActorRole(),
            Name:       request.Name,
            Email:      request.Email,
            Password:   request.Password,
            Role:       request.Role), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    // PUT /v1/team/{employeeId}
    [HttpPut("{employeeId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid employeeId,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken ct)
    {
        var result = await employees.UpdateAsync(new UpdateEmployeeCommand(
            MerchantId: GetMerchantId(),
            EmployeeId: employeeId,
            ActorRole:  GetActorRole(),
            Name:       request.Name,
            Role:       request.Role), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/team/{employeeId}/suspend
    [HttpPost("{employeeId:guid}/suspend")]
    [Authorize]
    public async Task<IActionResult> Suspend(Guid employeeId, CancellationToken ct)
    {
        var result = await employees.SuspendAsync(new SuspendEmployeeCommand(
            GetMerchantId(), employeeId, GetActorRole()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // POST /v1/team/{employeeId}/reactivate
    [HttpPost("{employeeId:guid}/reactivate")]
    [Authorize]
    public async Task<IActionResult> Reactivate(Guid employeeId, CancellationToken ct)
    {
        var result = await employees.ReactivateAsync(new ReactivateEmployeeCommand(
            GetMerchantId(), employeeId, GetActorRole()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    // ── Helpers ────────────────────────────────────────────────
 
    /// <summary>
    /// Résout le MerchantId depuis le JWT.
    /// Pour un owner : sub = merchantId.
    /// Pour un employé : claim "merchant_id".
    /// </summary>
    private Guid GetMerchantId()
    {
        // Employé : claim "merchant_id" injecté dans le token
        var merchantClaim = User.FindFirst("merchantId")?.Value;
        if (Guid.TryParse(merchantClaim, out var mId)) return mId;
 
        // Owner : sub = merchantId
        var sub = User.FindFirst("merchantId")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
 
    private Guid GetActorId()
    {
        var sub = User.FindFirst("merchantId")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
 
    private string GetActorRole()
        => User.FindFirst("role")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? "viewer";
}
 
// Request contracts 

public sealed record CreateEmployeeRequest(
    string Name,
    string Email,
    string Password,
    string Role);     // "developer" | "finance" | "support" | "viewer"
 
public sealed record UpdateEmployeeRequest(
    string? Name,
    string? Role);