namespace AfriPay.Domain.Merchants.ValueObjects;

/// <summary>
/// Informations KYB (Know Your Business) soumises par le marchand lors de la vérification de son identité légale.
/// Valeur immuable : toute correction nécessite la création d'une nouvelle instance.
/// </summary>
/// <param name="LegalName">Dénomination légale enregistrée auprès des autorités compétentes.</param>
/// <param name="RegistrationNumber">Numéro d'immatriculation officiel de l'entreprise.</param>
/// <param name="Country">Code pays ISO 3166-1 alpha-2 du pays d'enregistrement.</param>
/// <param name="SubmittedAt">Date de soumission du dossier KYB.</param>
public sealed record KybInfo(
    string LegalName,
    string RegistrationNumber,
    string Country,
    DateOnly SubmittedAt
);