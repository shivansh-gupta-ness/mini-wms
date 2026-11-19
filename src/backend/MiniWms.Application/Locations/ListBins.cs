using FluentValidation;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Common.Validation;

namespace MiniWms.Application.Locations;

public record ListBinsQuery(
    int Page = PagedQuery.DefaultPage,
    int PageSize = PagedQuery.DefaultPageSize,
    string? Sort = null,
    bool? IsActive = null,
    int? ZoneId = null,
    string? Q = null) : PagedQuery(Page, PageSize, Sort);

public class ListBinsValidator : AbstractValidator<ListBinsQuery>
{
    public ListBinsValidator()
    {
        Include(new PagedQueryValidator<BinDto>(LocationSortFields.Bins));
        RuleFor(c => c.ZoneId).GreaterThan(0).When(c => c.ZoneId is not null)
            .WithMessage("Zone id must be greater than 0.");
        RuleFor(c => c.Q).ValidSearch();
    }
}

public class ListBinsHandler(ILocationQueries queries)
{
    public async Task<PagedResult<BinDto>> HandleAsync(ListBinsQuery query, CancellationToken cancellationToken)
    {
        await new ListBinsValidator().ValidateAndThrowAsync(query, cancellationToken);

        var sort = SortParser.Parse(query.Sort, LocationSortFields.Bins, out _);
        return await queries.ListBinsAsync(
            new PagedQuery(query.Page, query.PageSize, query.Sort),
            sort,
            query.IsActive,
            query.ZoneId,
            LocationRules.NormalizeSearch(query.Q),
            cancellationToken);
    }
}
