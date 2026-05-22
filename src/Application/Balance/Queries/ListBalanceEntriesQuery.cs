using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Balance.Queries;

public sealed record ListBalanceEntriesQuery(
    Guid            MerchantId,
    string          Currency,
    DateTimeOffset? From     = null,
    DateTimeOffset? To       = null,
    int             Page     = 1,
    int             PageSize = 50);
 
public sealed class ListBalanceEntriesHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<BalanceEntryDto>>> HandleAsync(
        ListBalanceEntriesQuery query, CancellationToken ct = default)
    {
        var entries = await uow.BalanceEntries.GetByMerchantAsync(
            query.MerchantId,
            query.Currency,
            query.From,
            query.To,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 200),
            ct);
 
        var dtos = (IReadOnlyList<BalanceEntryDto>)entries
            .Select(BalanceEntryDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<BalanceEntryDto>>.Ok(dtos);
    }
}