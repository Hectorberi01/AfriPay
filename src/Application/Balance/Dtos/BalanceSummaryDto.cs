using AfriPay.Domain.Balance;

namespace AfriPay.Application.Balance.Dtos;

public sealed record BalanceSummaryDto(
    Guid   MerchantId,
    string Currency,
    long   AvailableBalance,
    long   PendingBalance,
    long   ReservedBalance,
    long   TotalBalance,
    string Status);
 
public sealed record BalanceEntryDto(
    string         EntryId,
    string         Type,
    string         Source,
    long           Amount,
    string         Currency,
    string?        ReferenceId,
    string?        Description,
    long           RunningBalance,
    DateTimeOffset CreatedAt)
{
    public static BalanceEntryDto FromDomain(BalanceEntry e) => new(
        e.Id.ToString(),
        e.Type.ToString().ToLower(),
        e.Source.ToString().ToLower(),
        e.Amount,
        e.Currency,
        e.ReferenceId,
        e.Description,
        e.RunningBalance,
        e.CreatedAt);
}