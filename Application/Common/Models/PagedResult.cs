namespace FinGround.Application.Common.Models;
// pagination envelope returned by any list query that supports
// server side paging.  Clients should use <see cref="TotalPages"/> and
// <see cref="HasNextPage"/> to drive "load more" or numbered-page UI controls

public record PagedResult<T>
{
    //The items on the current page
    public IReadOnlyList<T> Items { get; init; }

    //Total number of items matching the applied filters, across all pages
    public int TotalCount { get; init; }

    //The 1-based page number returned
    public int Page { get; init; }

    //Maximum number of items per page (as requested, clamped by the server)
    public int PageSize { get; init; }

    //Total number of pages given <see cref="TotalCount"/> and <see cref="PageSize"/>
    public int TotalPages => PageSize > 0
                                                 ? (int)Math.Ceiling((double)TotalCount / PageSize)
                                                 : 0;

    //true: when <see cref="Page"/> is greater than 1
    public bool HasPreviousPage => Page > 1;

    //true:when there is at least one more page after the current one
    public bool HasNextPage => Page < TotalPages;

    //Constructs a <see cref="PagedResult{T}"/>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
