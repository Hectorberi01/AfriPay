namespace AfriPay.Domain.Employees;

public enum EmployeeRole
{
    Owner,       // Accès total — créateur du compte marchand
    Developer,   // Clés API, webhooks, intégrations
    Finance,     // Paiements, remboursements, payouts, analytics
    Support,     // Lecture paiements, remboursements en lecture
    Viewer,      // Lecture seule sur tout
}
 
public enum EmployeeStatus
{
    Active,
    Suspended,
}
