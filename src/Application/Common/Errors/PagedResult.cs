namespace AfriPay.Application.Common.Errors;

/// <summary>Résultat paginé générique.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalCount,
    int              Page,
    int              PageSize,
    bool             HasMore
)
{
    public bool HasMore       => Page * PageSize < TotalCount;
    public int  TotalPages    => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string? NextCursor => HasMore ? Items.LastOrDefault()?.GetHashCode().ToString() : null;
}
