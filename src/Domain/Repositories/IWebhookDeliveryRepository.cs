using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries.ListPayments;
using AfriPay.Domain.Webhooks;

namespace AfriPay.Domain.Repositories;

public interface IWebhookDeliveryRepository : IRepository<WebhookDelivery>
{
    /// <summary>
    /// Retourne les livraisons dues pour dispatch immédiat.
    /// Critère : status IN (Pending, Retrying) AND next_retry_at &lt;= NOW()
    /// Index partiel sur (next_retry_at) WHERE status IN ('pending','retrying').
    /// Appelée par le WebhookDispatcherJob toutes les 10 secondes.
    /// </summary>
    Task<IReadOnlyList<WebhookDelivery>> GetDueForDeliveryAsync(
        int batchSize = 50, CancellationToken ct = default);
 
    Task<PagedResult<WebhookDelivery>> ListByMerchantAsync(
        Guid              merchantId,
        int               page,
        int               pageSize,
        CancellationToken ct = default);
 
    /// <summary>
    /// Charge la livraison avec toutes ses tentatives.
    /// Utilisé pour le debugging dans le dashboard.
    /// </summary>
    Task<WebhookDelivery?> GetWithAttemptsAsync(
        Guid deliveryId, CancellationToken ct = default);
 
    /// <summary>
    /// Compte les livraisons dead-letter d'un marchand.
    /// Utilisé pour les alertes et métriques.
    /// </summary>
    Task<int> CountDeadLetterAsync(
        Guid merchantId, CancellationToken ct = default);
}