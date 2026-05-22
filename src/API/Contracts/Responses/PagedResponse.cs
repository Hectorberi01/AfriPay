using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Responses;

/// <summary>Réponse paginée générique utilisée pour toutes les listes.</summary>
public sealed record PagedResponse<T>
{
    public IReadOnlyList<T> Data       { get; init; } = [];
    public int              TotalCount { get; init; }
    public int              Page       { get; init; }
    public int              PageSize   { get; init; }
    public bool             HasMore    { get; init; }
 
    public static PagedResponse<T> From(PagedResult<T> paged) =>
        new()
        {
            Data       = paged.Items,
            TotalCount = paged.TotalCount,
            Page       = paged.Page,
            PageSize   = paged.PageSize,
            HasMore    = paged.HasMore,
        };
}