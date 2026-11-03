using ProductManagement.Application.Common.Exceptions;

namespace ProductManagement.Application.Common.Pagination;

public sealed class PageRequest
{
    public PageRequest(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new InvalidPaginationException($"Page must be at least 1, but was {page}.");
        }

        if (pageSize < 1 || pageSize > PaginationConfig.MaxPageSize)
        {
            throw new InvalidPaginationException(
                $"Page size must be between 1 and {PaginationConfig.MaxPageSize}, but was {pageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int Skip => (Page - 1) * PageSize;
}