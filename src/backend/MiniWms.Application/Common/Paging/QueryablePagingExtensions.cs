using System.Linq.Expressions;
using MiniWms.Application.Common.Sorting;
using MiniWms.Domain.Common;

namespace MiniWms.Application.Common.Paging;

/// <summary>Sorting and paging over <see cref="IQueryable{T}"/> (System.Linq only; no EF Core here, ADR-001).</summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// Applies the sort (none when omitted; <c>Id</c> is always the final tie-breaker so pages are stable),
    /// then the page. Call <see cref="ToPagedResult{T}"/> when the items must be mapped by the caller.
    /// </summary>
    public static IQueryable<TEntity> ApplySort<TEntity>(
        this IQueryable<TEntity> source, SortSpec<TEntity>? sort)
        where TEntity : AuditableEntity
    {
        if (sort is null)
        {
            return source.OrderBy(e => e.Id);
        }

        var ordered = sort.Descending ? source.OrderByDescending(sort.Selector) : source.OrderBy(sort.Selector);
        return ordered.ThenBy(e => e.Id);
    }

    /// <summary>
    /// Same as <see cref="ApplySort{TEntity}(IQueryable{TEntity}, SortSpec{TEntity}?)"/> for a projected query
    /// (a DTO) that is not an entity; <paramref name="idSelector"/> is the final tie-breaker.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source, SortSpec<T>? sort, Expression<Func<T, int>> idSelector)
    {
        if (sort is null)
        {
            return source.OrderBy(idSelector);
        }

        var ordered = sort.Descending ? source.OrderByDescending(sort.Selector) : source.OrderBy(sort.Selector);
        return ordered.ThenBy(idSelector);
    }

    public static IQueryable<T> ApplyPage<T>(this IQueryable<T> source, PagedQuery query) =>
        source.Skip(SkipCount(query)).Take(query.PageSize);

    /// <summary>Rows before the page, computed in long so a huge page number cannot overflow into a wrong page.</summary>
    public static int SkipCount(PagedQuery query) =>
        (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);

    /// <summary>Counts, then reads one page. Run <see cref="ApplySort{TEntity}"/> first.</summary>
    public static PagedResult<T> ToPagedResult<T>(this IQueryable<T> sorted, PagedQuery query)
    {
        var total = sorted.Count();
        var items = sorted.ApplyPage(query).ToList();
        return new PagedResult<T>(items, query.Page, query.PageSize, total);
    }
}
