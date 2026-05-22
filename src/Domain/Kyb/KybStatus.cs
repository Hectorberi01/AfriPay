namespace AfriPay.Domain.Kyb;

public enum KybStatus
{
    NotStarted,     // Compte créé, KYB non initié
    Draft,          // En cours de remplissage
    Submitted,      // Soumis, en attente de review
    UnderReview,    // En cours d'examen par l'équipe AfriPay
    AdditionalInfoRequired,  // Documents supplémentaires demandés
    Approved,       // KYB validé — accès live débloqué
    Rejected,       // KYB refusé
    Expired,        // KYB expiré (renouvellement annuel)
}