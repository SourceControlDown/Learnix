namespace Learnix.Application.Common.Pagination;

public record PaginatedResult<TEntity>(
    int PageIndex,
    int PageSize,
    long TotalCount,
    IReadOnlyList<TEntity> Data
) where TEntity : class
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageIndex < TotalPages - 1;
    public bool HasPreviousPage => PageIndex > 0;

    public static PaginatedResult<TEntity> Create(
        IEnumerable<TEntity> items, int pageIndex, int pageSize, long totalCount)
        => new(pageIndex, pageSize, totalCount, items.ToList().AsReadOnly());

    public static PaginatedResult<TEntity> Empty(int pageIndex, int pageSize)
        => new(pageIndex, pageSize, 0, Array.Empty<TEntity>());
}