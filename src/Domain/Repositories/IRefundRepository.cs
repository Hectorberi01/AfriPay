using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries;
using AfriPay.Application.Payments.Queries.ListPayments;
using AfriPay.Domain.Refunds;

namespace AfriPay.Domain.Repositories;

public interface IRefundRepository : IRepository<Refund>
{
    Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(
        Guid paymentId, CancellationToken ct = default);
 
    Task<PagedResult<Refund>> ListByMerchantAsync(
        Guid              merchantId,
        RefundQueryFilter filter,
        int               page,
        int               pageSize,
        CancellationToken ct = default);
 
    /// <summary>
    /// Retourne les remboursements Pending et Processing.
    /// Utilisé par le RefundProcessorJob pour relancer les traitements incomplets.
    /// </summary>
    Task<IReadOnlyList<Refund>> GetPendingAndProcessingAsync(
        int batchSize = 100, CancellationToken ct = default);
 
    /// <summary>
    /// Calcule le total déjà remboursé pour un paiement.
    /// Utilisé pour valider qu'un nouveau remboursement ne dépasse pas le montant original.
    /// </summary>
    Task<long> GetTotalRefundedAmountAsync(
        Guid paymentId, CancellationToken ct = default);
}