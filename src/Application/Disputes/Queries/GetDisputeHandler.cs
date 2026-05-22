using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Disputes.Queries;

public sealed record GetDisputeQuery(Guid DisputeId, Guid MerchantId);
 
public sealed class GetDisputeHandler(IUnitOfWork uow)
{
    public async Task<Result<DisputeDto>> HandleAsync(
        GetDisputeQuery query, CancellationToken ct = default)
    {
        var dispute = await uow.Disputes.GetByIdAsync(query.DisputeId, ct);
        if (dispute is null)
            return Result<DisputeDto>.Fail(
                AppError.NotFound("Dispute", query.DisputeId.ToString()));
        if (dispute.MerchantId != query.MerchantId)
            return Result<DisputeDto>.Fail(AppError.Unauthorized());
        return Result<DisputeDto>.Ok(DisputeDto.FromDomain(dispute));
    }
}