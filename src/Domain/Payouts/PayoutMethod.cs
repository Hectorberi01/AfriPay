namespace AfriPay.Domain.Payouts;

public enum PayoutMethod
{
    MobileMoney,    // MTN MoMo, Wave, Moov, Orange
    BankTransfer,   // Virement bancaire
    Wallet,         // PayPal, autre wallet
}