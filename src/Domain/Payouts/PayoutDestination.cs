namespace AfriPay.Domain.Payouts;

public sealed class PayoutDestination
{
    public PayoutMethod  Method      { get; private set; }
    public string?       PhoneNumber { get; private set; }  // Mobile Money
    public string?       ProviderKey { get; private set; }  // "mtn_momo", "wave"…
    public BankAccount?  BankAccount { get; private set; }  // Virement bancaire
 
    private PayoutDestination() { }
 
    public static PayoutDestination ForMobileMoney(string providerKey, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new PayoutDomainException("Phone number is required for Mobile Money payout.");
 
        return new PayoutDestination
        {
            Method      = PayoutMethod.MobileMoney,
            ProviderKey = providerKey,
            PhoneNumber = phoneNumber,
        };
    }
 
    public static PayoutDestination ForBankTransfer(BankAccount account)
        => new()
        {
            Method      = PayoutMethod.BankTransfer,
            BankAccount = account,
        };
}