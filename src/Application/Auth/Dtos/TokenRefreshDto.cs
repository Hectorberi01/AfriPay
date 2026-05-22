namespace AfriPay.Application.Auth.Dtos;

public sealed record TokenRefreshDto(
    string AccessToken,
    string RefreshToken,
    int    ExpiresIn);