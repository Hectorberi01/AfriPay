using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;
using static AfriPay.Application.Balance.Dtos.BalanceDtoMapper;

namespace AfriPay.Application.Balance.Queries;

public sealed record ListBalancesQuery(Guid MerchantId);
 
public sealed class ListBalancesHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<BalanceSummaryDto>>> HandleAsync(
        ListBalancesQuery query, CancellationToken ct = default)
    {
        var balances = await uow.MerchantBalances
            .GetAllByMerchantAsync(query.MerchantId, ct);
 
        var dtos = (IReadOnlyList<BalanceSummaryDto>)balances
            .Select(ToDto).ToList();
 
        return Result<IReadOnlyList<BalanceSummaryDto>>.Ok(dtos);
    }
}