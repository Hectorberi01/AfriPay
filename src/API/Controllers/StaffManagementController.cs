using AfriPay.API.Extensions;
using AfriPay.Application.Staff;
using AfriPay.Application.Staff.Commands;
using AfriPay.Application.Staff.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/internal/staff")]
[Authorize(Policy = "StaffOnly")]
[Produces("application/json")]
public sealed class StaffManagementController(IStaffService staff) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? role   = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await staff.ListAsync(new ListStaffQuery(role, status), ct);
        return result.Match(onSuccess: Ok, onError: e => e.ToActionResult());
    }
 
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var actorId = GetStaffId();
        var result  = await staff.CreateAsync(new CreateStaffCommand(
            actorId, request.Name, request.Email,
            request.Password, request.Role, request.Department), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPut("{staffId:guid}/role")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ChangeRole(
        Guid staffId, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        var result = await staff.ChangeRoleAsync(
            new ChangeStaffRoleCommand(GetStaffId(), staffId, request.Role), ct);
 
        return result.Match(onSuccess: Ok, onError: e => e.ToActionResult());
    }
 
    [HttpPost("{staffId:guid}/suspend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Suspend(Guid staffId, CancellationToken ct)
    {
        var result = await staff.SuspendAsync(
            new SuspendStaffCommand(GetStaffId(), staffId), ct);
 
        return result.Match(onSuccess: Ok, onError: e => e.ToActionResult());
    }
 
    private Guid GetStaffId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public sealed record StaffLoginRequest(string Email, string Password);
public sealed record CreateStaffRequest(
    string Name, string Email, string Password,
    string Role, string? Department);
public sealed record ChangeRoleRequest(string Role);