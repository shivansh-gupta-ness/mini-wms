using FluentValidation;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;

namespace MiniWms.Application.Common.Validation;

/// <summary>Validates page, pageSize and the sort allow-list of a list query. Property names become camelCase fields.</summary>
public class PagedQueryValidator<TEntity> : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator(SortFields<TEntity> allowedSortFields)
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or more.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, PagedQuery.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PagedQuery.MaxPageSize}.");

        RuleFor(q => q.Sort)
            .Custom((sort, context) =>
            {
                SortParser.Parse(sort, allowedSortFields, out var error);
                if (error is not null)
                {
                    context.AddFailure(error);
                }
            });
    }
}
