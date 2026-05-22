using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Commands.CancelPayment;
using AfriPay.Application.Payments.Commands.InitiPayment;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Payments.Queries;
using AfriPay.Application.Payments.Queries.GetPayment;
using AfriPay.Application.Payments.Queries.ListPayments;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;

namespace AfriPay.Application.Payments;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalCount,
    int              Page,
    int              PageSize,
    bool             HasMore);


public interface IPaymentService
{
    Task<Result<PaymentDto>> InitiateAsync(InitiatePaymentDto dto, CancellationToken ct = default);
    Task<Result<PaymentDto>> GetByIdAsync(Guid paymentId, Guid merchantId, CancellationToken ct = default);
    Task<Result<PagedResult<PaymentDto>>> ListAsync(ListPaymentsQuery query, CancellationToken ct = default);
    Task<Result<PaymentDto>> CancelAsync(Guid paymentId, Guid merchantId, CancellationToken ct = default);
}


public sealed class PaymentService(
    InitiatePaymentHandler initiatePaymentHandler,
    CancelPaymentHandler cancelPaymentHandler,
    GetPaymentHandler getPaymentHandler,
    ListPaymentsHandler listPaymentsHandler
    ) : IPaymentService
{

    public async Task<Result<PaymentDto>> InitiateAsync(InitiatePaymentDto dto, CancellationToken ct = default)
        => await initiatePaymentHandler.Handle(dto, ct);
    
    public async Task<Result<PaymentDto>> GetByIdAsync(Guid paymentId, Guid merchantId, CancellationToken ct = default)
        => await getPaymentHandler.Handle(new GetPaymentQuery(paymentId, merchantId), ct);
    
    public async Task<Result<PagedResult<PaymentDto>>> ListAsync(ListPaymentsQuery query, CancellationToken ct = default)
        => await listPaymentsHandler.Handle(query, ct);
    
    public async Task<Result<PaymentDto>> CancelAsync(Guid paymentId, Guid merchantId, CancellationToken ct = default)
        => await cancelPaymentHandler.Handle(new CancelPaymentDto(paymentId, merchantId), ct);
    
}