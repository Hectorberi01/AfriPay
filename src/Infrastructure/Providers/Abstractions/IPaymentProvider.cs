namespace AfriPay.Infrastructure.Providers.Abstractions;

/// <summary>
/// Contrat que chaque adapter provider doit implémenter.
/// L'orchestrateur ne connaît que cette interface.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Identifiant du provider : "mtn_momo", "wave", "stripe", etc.</summary>
    string ProviderKey { get; }
 
    Task<ProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default);
 
    Task<ProviderResult> GetStatusAsync(string providerReference,bool isLive, CancellationToken ct = default);
 
    Task<ProviderResult> CancelAsync(string providerReference,bool isLive, CancellationToken ct = default);
 
    /// <summary>Health check léger — vérifie la disponibilité du provider.</summary>
    Task<bool> IsAvailableAsync(bool isLive,CancellationToken ct = default);
}