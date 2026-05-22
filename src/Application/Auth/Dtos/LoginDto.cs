namespace AfriPay.Application.Auth.Dtos;

public sealed record LoginDto(
    string AccessToken,
    string RefreshToken,
    int    ExpiresIn,      // secondes
    string TokenType,
    string MerchantId,
    string BusinessName,
    string Email,
    string Plan);