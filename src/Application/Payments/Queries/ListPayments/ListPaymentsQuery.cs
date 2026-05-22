using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using MediatR;

namespace AfriPay.Application.Payments.Queries.ListPayments;

public sealed record ListPaymentsQuery(
    Guid    MerchantId,
    string? Status      = null,
    string? ProviderKey = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To   = null,
    int     Page         = 1,
    int     PageSize     = 20
) : IRequest<Result<PagedResult<PaymentDto>>>;