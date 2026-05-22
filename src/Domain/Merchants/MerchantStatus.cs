namespace AfriPay.Domain.Merchants;

/// <summary>Représente les états possibles d'un marchand dans son cycle de vie sur la plateforme.</summary>
public enum MerchantStatus
{
    /// <summary>Compte créé mais KYB non encore validé. Les paiements sont bloqués.</summary>
    Pending,

    /// <summary>KYB validé. Le marchand peut traiter des paiements.</summary>
    Active,

    /// <summary>Compte temporairement désactivé par l'équipe AfriPay (fraude, litige, etc.).</summary>
    Suspended,

    /// <summary>Compte définitivement fermé. Aucune opération n'est possible.</summary>
    Closed,
}