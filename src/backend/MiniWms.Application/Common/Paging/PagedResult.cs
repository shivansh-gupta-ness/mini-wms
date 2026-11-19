namespace MiniWms.Application.Common.Paging;

/// <summary>A page of a list, serialised as <c>items</c>, <c>page</c>, <c>pageSize</c>, <c>totalItems</c> (ADR-003).</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems);
