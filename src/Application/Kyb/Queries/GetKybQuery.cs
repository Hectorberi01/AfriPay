using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Queries;

public sealed record GetKybQuery(Guid MerchantId);
 
public sealed class GetKybHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        GetKybQuery query, CancellationToken ct = default)
    {
        var kyb = await uow.Kyb.GetByMerchantAsync(query.MerchantId, ct);
 
        if (kyb is null)
            return Result<KybDto>.Fail(
                AppError.NotFound("KybApplication", query.MerchantId.ToString()));
 
        return Result<KybDto>.Ok(KybDto.FromDomain(kyb));
    }
}