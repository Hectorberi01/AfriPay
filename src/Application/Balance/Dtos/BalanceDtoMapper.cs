using AfriPay.Domain.Balance;


namespace AfriPay.Application.Balance.Dtos;

public static class BalanceDtoMapper
{
    public static BalanceSummaryDto ToDto(MerchantBalance b) => new(
        b.MerchantId,
        b.Currency,
        b.AvailableBalance,
        b.PendingBalance,
        b.ReservedBalance,
        b.TotalBalance,
        b.Status.ToString().ToLower());
}
