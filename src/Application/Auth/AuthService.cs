using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Auth.Dtos;
using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Auth;

 
public sealed class AuthService(
    RegisterHandler       registerHandler,
    LoginHandler          loginHandler,
    RefreshTokenHandler   refreshHandler,
    LogoutHandler         logoutHandler,
    ChangePasswordHandler changePasswordHandler) : IAuthService
{
    public Task<Result<RegisterDto>>     RegisterAsync(RegisterCommand cmd, CancellationToken ct = default)             => registerHandler.HandleAsync(cmd, ct);
    public Task<Result<LoginDto>>        LoginAsync(LoginCommand cmd, CancellationToken ct = default)              => loginHandler.HandleAsync(cmd, ct);
    public Task<Result<TokenRefreshDto>> RefreshAsync(RefreshTokenCommand cmd, CancellationToken ct = default)     => refreshHandler.HandleAsync(cmd, ct);
    public Task<Result<bool>>            LogoutAsync(LogoutCommand cmd, CancellationToken ct = default)            => logoutHandler.HandleAsync(cmd, ct);
    public Task<Result<bool>>            ChangePasswordAsync(ChangePasswordCommand cmd, CancellationToken ct = default) => changePasswordHandler.HandleAsync(cmd, ct);
}