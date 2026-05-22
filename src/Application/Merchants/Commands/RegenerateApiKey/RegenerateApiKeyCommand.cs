using AfriPay.Application.Common.Errors;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.RegenerateApiKey;

public sealed record RegenerateApiKeyCommand(Guid MerchantId, string KeyType)
    : IRequest<Result<RegenerateApiKeyResponse>>;
 
public sealed record RegenerateApiKeyResponse(
    string KeyType,
    string NewKey,
    string Warning = "Store this key securely. It will not be shown again.");