namespace AfriPay.Application.Payments.Queries;

/// <summary>Filtres pour la liste paginée des remboursements.</summary>
public sealed record RefundQueryFilter(
    string?         Status  = null,
    DateTimeOffset? From    = null,
    DateTimeOffset? To      = null
);