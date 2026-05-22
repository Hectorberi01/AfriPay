using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Auth.Dtos;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterDto>>     RegisterAsync(RegisterCommand cmd,            CancellationToken ct = default);
    Task<Result<LoginDto>>        LoginAsync(LoginCommand cmd,             CancellationToken ct = default);
    Task<Result<TokenRefreshDto>> RefreshAsync(RefreshTokenCommand cmd,    CancellationToken ct = default);
    Task<Result<bool>>            LogoutAsync(LogoutCommand cmd,           CancellationToken ct = default);
    Task<Result<bool>>            ChangePasswordAsync(ChangePasswordCommand cmd, CancellationToken ct = default);
}