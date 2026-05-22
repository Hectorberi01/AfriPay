using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;
using static AfriPay.Application.Balance.Dtos.BalanceDtoMapper;

namespace AfriPay.Application.Balance.Queries;

public sealed record GetBalanceQuery(Guid MerchantId, string Currency);
 
public sealed class GetBalanceHandler(IUnitOfWork uow)
{
    public async Task<Result<BalanceSummaryDto>> HandleAsync(
        GetBalanceQuery query, CancellationToken ct = default)
    {
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(query.MerchantId, query.Currency, ct);
 
        // Retourner un solde vide si pas encore de transactions
        if (balance is null)
            return Result<BalanceSummaryDto>.Ok(new BalanceSummaryDto(
                query.MerchantId,
                query.Currency.ToUpperInvariant(),
                0, 0, 0, 0, "active"));
 
        return Result<BalanceSummaryDto>.Ok(ToDto(balance));
    }
}