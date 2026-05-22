namespace AfriPay.Domain.Webhooks;

public enum DeliveryStatus
{
    /// <summary>En attente de première livraison.</summary>
    Pending,
 
    /// <summary>Au moins une tentative a échoué — en attente de retry.</summary>
    Retrying,
 
    /// <summary>Livré avec succès (réponse HTTP 2xx de l'app marchand).</summary>
    Delivered,
 
    /// <summary>Toutes les tentatives épuisées — envoyé en dead-letter.</summary>
    DeadLetter,
}