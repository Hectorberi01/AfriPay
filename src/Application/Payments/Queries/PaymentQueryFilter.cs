namespace AfriPay.Application.Payments.Queries;

/// <summary>Filtres pour la liste paginée des paiements.</summary>
public sealed record PaymentQueryFilter(
    string?         Status      = null,
    string?         ProviderKey = null,
    DateTimeOffset? From        = null,
    DateTimeOffset? To          = null
);