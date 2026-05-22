using AfriPay.API.Extensions;
using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Route("v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    //[AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var result = await auth.RegisterAsync(
            new RegisterCommand(
                request.BusinessName,
                request.Email,
                request.Password,
                request.Country), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
    
    [HttpPost("login")]
    //[AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(
            new LoginCommand(request.Email, request.Password), ct);

        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await auth.RefreshAsync(
            new RefreshTokenCommand(request.RefreshToken), ct);

        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var merchantId = User.FindFirst("merchantId")?.Value;

        if (string.IsNullOrEmpty(merchantId) || !Guid.TryParse(merchantId, out var id))
            return Unauthorized();

        await auth.LogoutAsync(new LogoutCommand(id), ct);
        return NoContent();
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var merchantId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("merchantI")?.Value;

        if (!Guid.TryParse(merchantId, out var id))
            return Unauthorized();

        var result = await auth.ChangePasswordAsync(
            new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword), ct);

        return result.Match(
            onSuccess: _ => NoContent(),
            onError:   error => error.ToActionResult());
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        return Ok(new
        {
            MerchantId = User.FindFirst("merchantId")?.Value,
            Email      = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            Plan       = User.FindFirst("plan")?.Value,
            Role       = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
        });
    }
}

// Request contracts

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record RegisterRequest(
    string BusinessName,
    string Email,
    string Password,
    string Country);