using FluentValidation;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Common.Validation;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

public record ListZonesQuery(
    int Page = PagedQuery.DefaultPage,
    int PageSize = PagedQuery.DefaultPageSize,
    string? Sort = null,
    bool? IsActive = null,
    string? Type = null,
    string? Q = null) : PagedQuery(Page, PageSize, Sort);

public class ListZonesValidator : AbstractValidator<ListZonesQuery>
{
    public ListZonesValidator()
    {
        Include(new PagedQueryValidator<Zone>(LocationSortFields.Zones));
        RuleFor(c => c.Type).ValidZoneType(required: false);
        RuleFor(c => c.Q).ValidSearch();
    }
}

public class ListZonesHandler(ILocationQueries queries)
{
    public async Task<PagedResult<ZoneDto>> HandleAsync(ListZonesQuery query, CancellationToken cancellationToken)
    {
        await new ListZonesValidator().ValidateAndThrowAsync(query, cancellationToken);

        var sort = SortParser.Parse(query.Sort, LocationSortFields.Zones, out _);
        ZoneType? type = query.Type is not null && LocationRules.TryParseZoneType(query.Type, out var parsed) ? parsed : null;
        return await queries.ListZonesAsync(
            new PagedQuery(query.Page, query.PageSize, query.Sort),
            sort,
            query.IsActive,
            type,
            LocationRules.NormalizeSearch(query.Q),
            cancellationToken);
    }
}
