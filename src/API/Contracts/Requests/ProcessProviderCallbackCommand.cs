using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record ProcessProviderCallbackCommand(
    string Provider,
    string RawBody,
    System.Collections.Generic.Dictionary<string, string> Headers)
    : MediatR.IRequest<Result<bool>>;