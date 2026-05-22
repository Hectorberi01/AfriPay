using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Payments.Queries.ListPayments;

public sealed class ListPaymentsHandler(IUnitOfWork uow) : IRequestHandler<ListPaymentsQuery, Result<PagedResult<PaymentDto>>>
{
    public async Task<Result<PagedResult<PaymentDto>>> Handle(ListPaymentsQuery q, CancellationToken ct)
    {
        var filter = new PaymentQueryFilter(q.Status, q.ProviderKey, q.From, q.To);
        var items = await uow.Payments.ListAsync(
            q.MerchantId, filter, q.Page, q.PageSize, ct);
 
        var dto = items.Items.Select(PaymentDto.FromDomain).ToList();
 
        return Result<PagedResult<PaymentDto>>.Ok(new PagedResult<PaymentDto>(
            dto, dto.Count, q.Page, q.PageSize,
            HasMore: q.Page * q.PageSize < dto.Count
        ));
    }
}