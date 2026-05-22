namespace AfriPay.Application.Auth.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool   Verify(string password, string hash);
}