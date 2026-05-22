namespace AfriPay.API.Middleware;

/// <summary>
/// Clés utilisées pour stocker le contexte marchand dans <see cref="HttpContext.Items"/>.
/// Centralisé ici pour éviter les magic strings dispersées dans les middlewares et controllers.
/// </summary>
public static class HttpContextKeys
{
    public const string MerchantId    = "AfriPay.MerchantId";
    public const string MerchantPlan  = "AfriPay.MerchantPlan";
    public const string RatePerMinute = "AfriPay.RatePerMinute";
    public const string WebhookUrl    = "AfriPay.WebhookUrl";
    public const string WebhookSecret = "AfriPay.WebhookSecret";
    public const string IsLive        = "AfriPay.IsLive";    
}

/// <summary>
/// Extensions sur <see cref="HttpContext"/> pour lire le contexte marchand
/// posé par <see cref="ApiKeyAuthMiddleware"/>.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Retourne l'ID du marchand authentifié.
    /// Lance une exception si ApiKeyAuthMiddleware n'a pas été exécuté avant.
    /// </summary>
    public static Guid GetMerchantId(this HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(HttpContextKeys.MerchantId, out var value)
            && value is Guid id)
            return id;

        throw new InvalidOperationException(
            "MerchantId not found in HttpContext.Items. " +
            "Ensure ApiKeyAuthMiddleware runs before the controller.");
    }

    /// <summary>Retourne le plan tarifaire du marchand ("starter", "growth", "scale").</summary>
    public static string GetMerchantPlan(this HttpContext ctx)
        => ctx.Items.TryGetValue(HttpContextKeys.MerchantPlan, out var v) && v is string plan
            ? plan
            : "starter";

    /// <summary>Retourne l'URL webhook configurée par le marchand, si présente.</summary>
    public static string? GetWebhookUrl(this HttpContext ctx)
        => ctx.Items.TryGetValue(HttpContextKeys.WebhookUrl, out var v) ? v as string : null;

    /// <summary>Retourne le secret webhook du marchand, si présent.</summary>
    public static string? GetWebhookSecret(this HttpContext ctx)
        => ctx.Items.TryGetValue(HttpContextKeys.WebhookSecret, out var v) ? v as string : null;
    
    /// <summary>
    /// Retourne true si la clé utilisée est une clé live (afp_live_sk_...).
    /// False si c'est une clé sandbox (afp_test_sk_...).
    /// </summary>
    public static bool GetIsLive(this HttpContext ctx)
    {
        var ct = ctx.Items.TryGetValue(HttpContextKeys.IsLive, out var v) && v is bool b && b;
        return ct;
    }
}