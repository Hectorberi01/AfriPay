using AfriPay.Domain.Merchants;

namespace AfriPay.Domain.Repositories;

public interface IMerchantRepository : IRepository<Merchant>
{
    /// <summary>
    /// Charge le marchand depuis le hash SHA-256 de sa clé API.
    /// Chemin chaud de chaque requête HTTP — index sur api_keys.key_hash.
    /// </summary>
    Task<Merchant?> GetByApiKeyHashAsync(string keyHash, CancellationToken ct = default);
 
    Task<Merchant?> GetByEmailAsync(string email, CancellationToken ct = default);
 
    /// <summary>Vérifie l'existence d'un email sans charger l'agrégat.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
 
    Task<IReadOnlyList<Merchant>> GetByCountryAsync(string country, CancellationToken ct = default);
 
    Task<IReadOnlyList<Merchant>> GetActiveByPlanAsync(PricingPlan plan, CancellationToken ct = default);
    
    Task<IReadOnlyList<Merchant>> ListAllAsync(
        MerchantStatus? status   = null,
        int             page     = 1,
        int             pageSize = 20,
        CancellationToken ct     = default);

    Task<Merchant?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
}
