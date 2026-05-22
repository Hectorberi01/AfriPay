namespace AfriPay.Domain.Payouts;

public sealed class BankAccount
{
    public string  Iban          { get; private set; } = default!;
    public string  Bic           { get; private set; } = default!;
    public string  AccountHolder { get; private set; } = default!;
    public string  BankName      { get; private set; } = default!;
    public string  Country       { get; private set; } = default!;
 
    private BankAccount() { }
 
    public static BankAccount Create(
        string iban, string bic, string accountHolder,
        string bankName, string country)
    {
        if (string.IsNullOrWhiteSpace(iban))
            throw new PayoutDomainException("IBAN is required.");
        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new PayoutDomainException("Account holder name is required.");
 
        return new BankAccount
        {
            Iban          = iban.Replace(" ", "").ToUpperInvariant(),
            Bic           = bic.ToUpperInvariant(),
            AccountHolder = accountHolder,
            BankName      = bankName,
            Country       = country.ToUpperInvariant(),
        };
    }
}
