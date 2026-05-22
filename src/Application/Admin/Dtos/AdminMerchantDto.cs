namespace AfriPay.Application.Admin.Dtos;

public sealed record AdminMerchantDto(
    string          MerchantId,
    string          BusinessName,
    string          Email,
    string          Country,
    string          Status,
    string          Plan,
    bool            KybApproved,
    DateTimeOffset  CreatedAt);