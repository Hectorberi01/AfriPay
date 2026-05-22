using AfriPay.Domain.Currency;

namespace AfriPay.Domain.Repositories;

public interface IExchangeRateRepository : IRepository<ExchangeRate>
{
    /// <summary>
    /// Dernier taux non expiré pour une paire de devises.
    /// Retourne null si aucun taux valide — le service doit en obtenir un nouveau.
    /// </summary>
    Task<ExchangeRate?> GetLatestValidAsync(CurrencyPair pair, CancellationToken ct = default);
 
    /// <summary>
    /// Dernier taux enregistré (même expiré).
    /// Utilisé comme fallback d'urgence si l'API de taux est indisponible.
    /// </summary>
    Task<ExchangeRate?> GetLastRecordedAsync(CurrencyPair pair, CancellationToken ct = default);
 
    /// <summary>
    /// Historique des taux sur une période.
    /// Utilisé pour la réconciliation comptable :
    /// retrouver le taux appliqué à une transaction passée.
    /// </summary>
    Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(
        CurrencyPair    pair,
        DateTimeOffset  since,
        DateTimeOffset? until           = null,
        CancellationToken ct = default);
 
    /// <summary>
    /// Taux appliqué à un instant précis (pour réconciliation).
    /// Retourne le taux le plus récent enregistré avant ou à l'instant donné.
    /// </summary>
    Task<ExchangeRate?> GetAtInstantAsync(
        CurrencyPair    pair,
        DateTimeOffset  at,
        CancellationToken ct = default);
}
