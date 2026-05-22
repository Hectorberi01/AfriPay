namespace AfriPay.Domain.Kyb;

public enum DocumentType
{
    BusinessRegistration,   // RCCM, Kbis, etc.
    TaxId,                  // Numéro fiscal
    OwnerIdentity,          // CNI, passeport du dirigeant
    BankStatement,          // Relevé bancaire
    ProofOfAddress,         // Justificatif de domicile
    SignedContract,         // Contrat AfriPay signé
}