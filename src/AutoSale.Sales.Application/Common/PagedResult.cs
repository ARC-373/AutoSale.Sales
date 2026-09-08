namespace AutoSale.Application.Common;

public sealed record PagedResult<TItem>(IReadOnlyCollection<TItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
