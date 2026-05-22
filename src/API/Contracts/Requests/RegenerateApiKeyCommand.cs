using AfriPay.API.Controllers;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record RegenerateApiKeyCommand(Guid MerchantId, string KeyType)
    : MediatR.IRequest<Result<RegenerateApiKeyResponse>>;