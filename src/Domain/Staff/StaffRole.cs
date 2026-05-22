namespace AfriPay.Domain.Staff;

public enum StaffRole
{
    SuperAdmin,   // Accès total — gère les autres admins
    Admin,        // KYB review, disputes, suspension marchands
    Finance,      // Analytics globales, payouts, fee management
    Support,      // Lecture marchands, paiements, disputes
    Developer,    // Config providers, monitoring, logs techniques
}
 
public enum StaffStatus
{
    Active,
    Suspended,
}