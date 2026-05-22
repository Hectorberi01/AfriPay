namespace AfriPay.Infrastructure.Providers.Abstractions;

public interface IPaymentProviderFactory
{
    /// <summary>
    /// Résout le provider à utiliser.
    /// Si providerKey == "auto", active le smart routing par préfixe téléphonique.
    /// </summary>
    IPaymentProvider Resolve(string providerKey, string? phoneNumber = null);
}