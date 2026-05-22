using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries;
using AfriPay.Domain.Payments;

namespace AfriPay.Domain.Repositories;



public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>
    /// Lookup d'idempotence principal.
    /// Retourne null si inexistant → créer.
    /// Non-null → retourner le résultat existant sans appeler le provider.
    /// Index UNIQUE sur (merchant_id, idempotency_key) → O(1).
    /// </summary>
    Task<Payment?> GetByIdempotencyKeyAsync(Guid   merchantId, string idempotencyKey, CancellationToken ct = default);
 
    /// <summary>
    /// Lookup par référence provider pour la réception des webhooks entrants.
    /// Ex : "MTN-TXN-8827361" → payment correspondant.
    /// </summary>
    Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken ct = default);
 
    /// <summary>
    /// Liste paginée avec filtres optionnels.
    /// Utilisé par le dashboard marchand et l'export CSV.
    /// </summary>
    Task<PagedResult<Payment>> ListAsync(
        Guid               merchantId,
        PaymentQueryFilter filter,
        int                page,
        int                pageSize,
        CancellationToken  ct = default);
 
    /// <summary>
    /// Retourne les paiements Pending dont expires_at est dépassé.
    /// Appelée par le job ExpiryJob toutes les 5 minutes.
    /// Limité à 200 résultats par batch pour éviter les locks prolongés.
    /// </summary>
    Task<IReadOnlyList<Payment>> GetExpiredPendingAsync(
        int batchSize = 200, CancellationToken ct = default);
 
    /// <summary>
    /// Statistiques agrégées pour le dashboard.
    /// Calculées côté base de données (GROUP BY) pour performance.
    /// </summary>
    Task<PaymentStats> GetStatsAsync(
        Guid            merchantId,
        DateTimeOffset  from,
        DateTimeOffset  to,
        CancellationToken ct = default);
 
    /// <summary>
    /// Charge le paiement avec son historique complet (transitions + tentatives).
    /// Utilisé pour l'affichage détail et le debugging.
    /// </summary>
    Task<Payment?> GetWithFullHistoryAsync(
        Guid paymentId, CancellationToken ct = default);
    
    /// <summary>
    /// Paiements Completed avant la date cutoff dont le solde n'a pas encore été settlé.
    /// Appelée par SettlementJob (T+1 — 24h après complétion).
    /// </summary>
    Task<IReadOnlyList<Payment>> GetSettlementPendingAsync(
        DateTimeOffset cutoff, CancellationToken ct = default);
}