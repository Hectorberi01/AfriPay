namespace AfriPay.Domain.Balance;

public enum BalanceStatus
{
    Active,
    Frozen,     // Bloqué suite à une suspicion de fraude
    Closed,
}