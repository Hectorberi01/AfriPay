using AfriPay.Infrastructure.Providers.Abstractions;

namespace AfriPay.Infrastructure.Providers;

public sealed class PaymentProviderFactory(
    IEnumerable<IPaymentProvider>          providers,
    ILogger<PaymentProviderFactory> logger)
    : IPaymentProviderFactory
{
    private readonly Dictionary<string, IPaymentProvider> _registry =
        providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
 
    public IPaymentProvider Resolve(string providerKey, string? phoneNumber = null)
    {
        if (!providerKey.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            if (_registry.TryGetValue(providerKey, out var explicit_))
                return explicit_;
 
            throw new InvalidOperationException(
                $"Provider '{providerKey}' is not registered. " +
                $"Available: {string.Join(", ", _registry.Keys)}");
        }
 
        // Smart Routing
        var detected = DetectProvider(phoneNumber);
 
        logger.LogInformation(
            "Smart routing: phone={Phone} → provider={Provider}",
            phoneNumber ?? "none", detected);
 
        if (_registry.TryGetValue(detected, out var auto))
            return auto;
 
        // Fallback ultime si le provider détecté n'est pas encore enregistré
        logger.LogWarning(
            "Detected provider '{Provider}' not registered. Falling back to stripe.",
            detected);
 
        return _registry.TryGetValue("stripe", out var stripe)
            ? stripe
            : throw new InvalidOperationException("No provider available.");
    }
 
    private static string DetectProvider(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "stripe";
 
        var digits = phone.TrimStart('+');
 
        return digits switch
        {
            // Bénin MTN MoMo (+229 61-65)
            var d when d.StartsWith("22961") || d.StartsWith("22962") ||
                       d.StartsWith("22963") || d.StartsWith("22964") ||
                       d.StartsWith("22965") => "mtn_momo",
 
            // Bénin Moov (+229 66-67, 94-95)
            var d when d.StartsWith("22966") || d.StartsWith("22967") ||
                       d.StartsWith("22994") || d.StartsWith("22995") => "moov_money",
 
            // Côte d'Ivoire MTN (+225 07, 57, 67)
            var d when d.StartsWith("22507") || d.StartsWith("22557") ||
                       d.StartsWith("22567") => "mtn_momo",
 
            // Sénégal Wave (+221 76, 70)
            var d when d.StartsWith("22176") || d.StartsWith("22170") => "wave",
 
            // Sénégal Orange Money (+221 77, 78)
            var d when d.StartsWith("22177") || d.StartsWith("22178") => "orange_money",
 
            // France / Europe
            var d when d.StartsWith("33") || d.StartsWith("32") => "stripe",
 
            _ => "stripe",
        };
    }
}