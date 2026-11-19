using System.Text.Json;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Common.Validation;

namespace MiniWms.Application.Tests.Common;

/// <summary>TASK-006 AC2: one allowed sort field, ascending or descending, Id tie-breaker, unknown field rejected.</summary>
public class SortingTests
{
    private static readonly SortFields<ListItem> Allowed = ListItem.AllowedSortFields;

    private static readonly ListItem[] Items =
    [
        new() { Id = 1, Name = "pear" },
        new() { Id = 2, Name = "apple" },
        new() { Id = 3, Name = "pear" },
        new() { Id = 4, Name = "banana" },
    ];

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Sort_by_name_orders_ascending_with_Id_breaking_ties()
    {
        Assert.Equal([2, 4, 1, 3], Sorted("name"));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Sort_by_minus_name_orders_descending_with_Id_still_ascending_for_ties()
    {
        Assert.Equal([1, 3, 4, 2], Sorted("-name"));
    }

    [Theory]
    [Trait("AC", "TASK-006.2")]
    [InlineData("NAME")]
    [InlineData("Name")]
    public void The_sort_field_is_case_insensitive(string sort)
    {
        Assert.Equal([2, 4, 1, 3], Sorted(sort));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Omitted_sort_keeps_Id_order()
    {
        var spec = SortParser.Parse(null, Allowed, out var error);

        Assert.Null(spec);
        Assert.Null(error);
        Assert.Equal([1, 2, 3, 4], Items.Reverse().AsQueryable().ApplySort(spec).Select(i => i.Id));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Blank_sort_is_treated_as_omitted()
    {
        var spec = SortParser.Parse("  ", Allowed, out var error);

        Assert.Null(spec);
        Assert.Null(error);
    }

    [Theory]
    [Trait("AC", "TASK-006.2")]
    [InlineData("colour")]
    [InlineData("-colour")]
    [InlineData("-")]
    public void An_unknown_field_is_rejected_with_a_message(string sort)
    {
        var spec = SortParser.Parse(sort, Allowed, out var error);

        Assert.Null(spec);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Two_fields_are_rejected_because_one_field_only_is_allowed()
    {
        var spec = SortParser.Parse("name,-quantity", Allowed, out var error);

        Assert.Null(spec);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [Trait("AC", "TASK-006.2")]
    [InlineData("colour")]
    [InlineData("name,quantity")]
    public void The_validator_reports_a_bad_sort_as_a_field_error_on_sort(string sort)
    {
        var result = new PagedQueryValidator<ListItem>(Allowed).Validate(new PagedQuery(1, 25, sort));

        var failure = Assert.Single(result.Errors);
        Assert.Equal("sort", JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName));
    }

    [Theory]
    [Trait("AC", "TASK-006.2")]
    [InlineData("name")]
    [InlineData("-quantity")]
    public void The_validator_accepts_an_allowed_sort(string sort)
    {
        var result = new PagedQueryValidator<ListItem>(Allowed).Validate(new PagedQuery(1, 25, sort));

        Assert.True(result.IsValid);
    }

    private static int[] Sorted(string sort)
    {
        var spec = SortParser.Parse(sort, Allowed, out _);
        return Items.Reverse().AsQueryable().ApplySort(spec).Select(i => i.Id).ToArray();
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void A_space_after_the_minus_sign_is_ignored()
    {
        var spec = SortParser.Parse("- name", Allowed, out var error);

        Assert.Null(error);
        Assert.NotNull(spec);
        Assert.True(spec.Descending);
        Assert.Equal("name", spec.Field);
    }

    [Fact]
    [Trait("AC", "TASK-006.1")]
    public void A_huge_page_number_does_not_overflow_the_skip_count()
    {
        Assert.Equal(int.MaxValue, QueryablePagingExtensions.SkipCount(new PagedQuery(int.MaxValue, 100)));
        Assert.Equal(50, QueryablePagingExtensions.SkipCount(new PagedQuery(3, 25)));
    }
}
