namespace MiniWms.Application.Common.Paging;

/// <summary>Paging and sorting input of every list endpoint (ADR-003). <see cref="Sort"/> is <c>field</c> or <c>-field</c>.</summary>
public record PagedQuery(int Page = PagedQuery.DefaultPage, int PageSize = PagedQuery.DefaultPageSize, string? Sort = null)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
}
