using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record GetMerchantQuery(Guid MerchantId)
    : MediatR.IRequest<Result<MerchantResponse>>;