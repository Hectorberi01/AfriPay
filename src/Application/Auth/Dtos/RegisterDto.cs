namespace AfriPay.Application.Auth.Dtos;

public sealed record RegisterDto(
    string MerchantId,
    string LiveApiKey,
    string SandboxApiKey,
    string AccessToken,
    string RefreshToken,
    int    ExpiresIn,
    string Warning);
