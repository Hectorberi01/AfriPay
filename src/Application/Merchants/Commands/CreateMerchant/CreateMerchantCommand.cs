using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.CreateMerchant;

public sealed record CreateMerchantCommand(
    string BusinessName,
    string Email,
    string Country)
    : IRequest<Result<CreateMerchantResponse>>;