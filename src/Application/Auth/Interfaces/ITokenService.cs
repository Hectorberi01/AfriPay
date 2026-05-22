namespace AfriPay.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid merchantId, string email, string plan, string role = "merchant");
    string GenerateRefreshToken();
}