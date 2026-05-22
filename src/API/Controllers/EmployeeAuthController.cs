using AfriPay.API.Extensions;
using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/auth/employee")]
[Produces("application/json")]
public sealed class EmployeeAuthController(IEmployeeService employees) : ControllerBase
{
    // POST /v1/auth/employee/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] EmployeeLoginRequest request,
        CancellationToken ct)
    {
        var result = await employees.LoginAsync(
            new EmployeeLoginCommand(request.Email, request.Password), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}
public sealed record EmployeeLoginRequest(string Email, string Password);
