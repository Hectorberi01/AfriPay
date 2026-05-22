using AfriPay.API.Extensions;
using AfriPay.Application.Staff;
using AfriPay.Application.Staff.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class StaffAuthController(IStaffService staff) : ControllerBase
{
    // POST /v1/auth/staff/login
    [HttpPost("v1/auth/staff/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] StaffLoginRequest request, CancellationToken ct)
    {
        var result = await staff.LoginAsync(
            new StaffLoginCommand(request.Email, request.Password), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}