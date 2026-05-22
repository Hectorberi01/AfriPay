using AfriPay.Domain.Exceptions;

namespace AfriPay.Domain.Balance;

public class BalanceEntry
{
    public Guid            Id           { get; private set; }
    public Guid            MerchantId   { get; private set; }
    public EntryType       Type         { get; private set; }
    public EntrySource     Source       { get; private set; }
    public long            Amount       { get; private set; }  // unités minimales
    public string          Currency     { get; private set; } = default!;
    public string?         ReferenceId  { get; private set; } // PaymentId, RefundId…
    public string?         Description  { get; private set; }
    public DateTimeOffset  CreatedAt    { get; private set; }
 
    // Solde courant après cette entrée (dénormalisé pour perf)
    public long            RunningBalance { get; private set; }
 
    private BalanceEntry() { }

    public static BalanceEntry CreateCredit(
        Guid         merchantId,
        EntrySource  source,
        long         amount,
        string       currency,
        string?      referenceId  = null,
        string?      description  = null,
        long         runningBalance = 0)
    {
        if (amount <= 0)
            throw new DomainException("Credit amount must be positive.");
 
        return new BalanceEntry
        {
            Id             = Guid.NewGuid(),
            MerchantId     = merchantId,
            Type           = EntryType.Credit,
            Source         = source,
            Amount         = amount,
            Currency       = currency.ToUpperInvariant(),
            ReferenceId    = referenceId,
            Description    = description ?? $"{source} credit",
            CreatedAt      = DateTimeOffset.UtcNow,
            RunningBalance = runningBalance,
        };
    }
    
    public static BalanceEntry CreateDebit(
        Guid         merchantId,
        EntrySource  source,
        long         amount,
        string       currency,
        string?      referenceId  = null,
        string?      description  = null,
        long         runningBalance = 0)
    {
        if (amount <= 0)
            throw new DomainException("Debit amount must be positive.");
 
        return new BalanceEntry
        {
            Id             = Guid.NewGuid(),
            MerchantId     = merchantId,
            Type           = EntryType.Debit,
            Source         = source,
            Amount         = amount,
            Currency       = currency.ToUpperInvariant(),
            ReferenceId    = referenceId,
            Description    = description ?? $"{source} debit",
            CreatedAt      = DateTimeOffset.UtcNow,
            RunningBalance = runningBalance,
        };
    }
}