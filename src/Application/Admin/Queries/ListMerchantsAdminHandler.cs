using AfriPay.Application.Admin.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Admin.Queries;

public sealed record ListMerchantsAdminQuery(
    string? Status   = null,
    string? Plan     = null,
    int     Page     = 1,
    int     PageSize = 20);
 
public sealed class ListMerchantsAdminHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<AdminMerchantDto>>> HandleAsync(
        ListMerchantsAdminQuery query, CancellationToken ct = default)
    {
        MerchantStatus? status = query.Status is not null
                                 && Enum.TryParse<MerchantStatus>(query.Status, ignoreCase: true, out var s)
            ? s : null;
 
        var merchants = await uow.Merchants.ListAllAsync(
            status,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 100), ct);
 
        var dtos = new List<AdminMerchantDto>();
        foreach (var m in merchants)
        {
            var kyb = await uow.Kyb.GetByMerchantAsync(m.Id, ct);
            dtos.Add(new AdminMerchantDto(
                m.Id.ToString(), m.BusinessName, m.Email, m.Country,
                m.Status.ToString().ToLower(), m.Plan.ToString().ToLower(),
                kyb?.LiveAccessAllowed ?? false, m.CreatedAt));
        }
 
        return Result<IReadOnlyList<AdminMerchantDto>>.Ok(dtos);
    }
}