namespace AfriPay.Domain.Merchants;

/// <summary>Plan tarifaire souscrit par un marchand, qui détermine ses limites opérationnelles.</summary>
public enum PricingPlan
{
    /// <summary>Plan d'entrée : volume mensuel limité, accès restreint aux fournisseurs de paiement.</summary>
    Starter,

    /// <summary>Plan intermédiaire : volume élevé et accès à tous les fournisseurs.</summary>
    Growth,

    /// <summary>Plan entreprise : volume et débit illimités, accès complet.</summary>
    Scale,
}