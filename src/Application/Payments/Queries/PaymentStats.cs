namespace AfriPay.Application.Payments.Queries;

/// <summary>Statistiques agrégées de paiements (pour le dashboard).</summary>
public sealed record PaymentStats(
    long   GrossVolume,
    long   TotalFees,
    long   NetVolume,
    int    TransactionCount,
    int    CompletedCount,
    int    FailedCount,
    double SuccessRatePct
);