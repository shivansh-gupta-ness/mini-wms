using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Common.Paging;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Asynchronous paging over EF Core queries (the Application helper stays free of EF Core, ADR-001).</summary>
public static class PagedQueryableExtensions
{
    /// <summary>Counts, then reads one page, without blocking a thread. Run <c>ApplySort</c> first.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> sorted, PagedQuery query, CancellationToken cancellationToken = default)
    {
        var total = await sorted.CountAsync(cancellationToken);
        var items = await sorted.ApplyPage(query).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, query.Page, query.PageSize, total);
    }
}
