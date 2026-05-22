using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Merchants.Queries.GetMerchant;

public sealed record GetMerchantQuery(Guid MerchantId) : IRequest<Result<MerchantResponse>>;
 
public sealed class GetMerchantHandler(IUnitOfWork uow) : IRequestHandler<GetMerchantQuery, Result<MerchantResponse>>
{
    public async Task<Result<MerchantResponse>> Handle(GetMerchantQuery  query, CancellationToken cancellationToken)
    {
        var merchant = await uow.Merchants.GetByIdAsync(query.MerchantId, cancellationToken);
 
        if (merchant is null)
            return Result<MerchantResponse>.Fail(
                AppError.NotFound("Merchant", query.MerchantId.ToString()));
 
        return Result<MerchantResponse>.Ok(new MerchantResponse
        {
            MerchantId   = merchant.Id.ToString(),
            BusinessName = merchant.BusinessName,
            Email        = merchant.Email,
            Country      = merchant.Country,
            Status       = merchant.Status.ToString().ToLower(),
            Plan         = merchant.Plan.ToString().ToLower(),
            HasWebhook   = merchant.WebhookConfig is not null,
            KybVerified  = merchant.KybInfo is not null,
            CreatedAt    = merchant.CreatedAt,
            VerifiedAt   = merchant.VerifiedAt,
        });
    }
}