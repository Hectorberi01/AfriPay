using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record ConvertCurrencyQuery(long Amount, string From, string To)
    : MediatR.IRequest<Result<ConvertResponse>>;