namespace AfriPay.Domain.Fee;

public sealed record FeeCalculation(
    long    FeeAmount,
    long    NetAmount, 
    decimal RateApplied,
    string  Currency,
    string  RuleDescription)
{
    public static FeeCalculation Zero(long amount, string currency)
        => new(0, amount, 0m, currency, "No fee rule matched");
 
    public static FeeCalculation From(FeeRule rule, long amount, string currency)
    {
        var fee = rule.Calculate(amount);
        return new FeeCalculation(
            FeeAmount:       fee,
            NetAmount:       amount - fee,
            RateApplied:     rule.RatePercent,
            Currency:        currency,
            RuleDescription: $"{rule.Type} {rule.RatePercent}% + {rule.FixedAmount} {currency}");
    }
}