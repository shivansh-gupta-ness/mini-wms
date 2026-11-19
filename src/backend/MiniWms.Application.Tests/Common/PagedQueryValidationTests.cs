using System.Text.Json;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Common.Validation;
using MiniWms.Domain.Common;

namespace MiniWms.Application.Tests.Common;

internal sealed class ListItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public static SortFields<ListItem> AllowedSortFields { get; } =
        new SortFields<ListItem>().Add("name", i => i.Name).Add("quantity", i => i.Quantity);
}

/// <summary>TASK-006 AC1 (paging defaults and limits) and AC3 (serialised shape and page slicing).</summary>
public class PagedQueryValidationTests
{
    private static readonly PagedQueryValidator<ListItem> Validator = new(ListItem.AllowedSortFields);

    // AC1
    [Fact]
    [Trait("AC", "TASK-006.1")]
    public void Omitted_page_and_pageSize_default_to_page_1_with_25_items()
    {
        var query = new PagedQuery();

        Assert.Equal(1, query.Page);
        Assert.Equal(25, query.PageSize);
        Assert.Null(query.Sort);
    }

    [Fact]
    [Trait("AC", "TASK-006.1")]
    public void The_default_query_is_valid()
    {
        var result = Validator.Validate(new PagedQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("AC", "TASK-006.1")]
    [InlineData(1)]
    [InlineData(100)]
    public void PageSize_at_the_limits_is_accepted(int pageSize)
    {
        var result = Validator.Validate(new PagedQuery(1, pageSize));

        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("AC", "TASK-006.1")]
    [InlineData(101)]
    [InlineData(0)]
    [InlineData(-5)]
    public void PageSize_outside_1_to_100_is_rejected_with_an_error_on_pageSize(int pageSize)
    {
        var result = Validator.Validate(new PagedQuery(1, pageSize));

        var failure = Assert.Single(result.Errors);
        Assert.Equal("pageSize", JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName));
        Assert.False(string.IsNullOrWhiteSpace(failure.ErrorMessage));
    }

    [Theory]
    [Trait("AC", "TASK-006.1")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_below_1_is_rejected_with_an_error_on_page(int page)
    {
        var result = Validator.Validate(new PagedQuery(page, 25));

        var failure = Assert.Single(result.Errors);
        Assert.Equal("page", JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName));
    }

    [Fact]
    [Trait("AC", "TASK-006.1")]
    public void Page_1_is_accepted()
    {
        var result = Validator.Validate(new PagedQuery(1, 25));

        Assert.True(result.IsValid);
    }

    // AC3
    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void Serialised_with_web_defaults_the_result_has_exactly_items_page_pageSize_totalItems()
    {
        var result = new PagedResult<string>(["a", "b"], 2, 10, 12);

        var json = JsonSerializer.SerializeToElement(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(["items", "page", "pageSize", "totalItems"], json.EnumerateObject().Select(p => p.Name));
        Assert.Equal(2, json.GetProperty("items").GetArrayLength());
        Assert.Equal(2, json.GetProperty("page").GetInt32());
        Assert.Equal(10, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(12, json.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void An_empty_result_serialises_items_as_an_empty_array()
    {
        var result = new PagedResult<string>([], 1, 25, 0);

        var json = JsonSerializer.SerializeToElement(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(JsonValueKind.Array, json.GetProperty("items").ValueKind);
        Assert.Equal(0, json.GetProperty("items").GetArrayLength());
        Assert.Equal(0, json.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void A_middle_page_holds_its_slice_and_totalItems_counts_all_rows()
    {
        var result = Numbered(7).ApplySort(null).ToPagedResult(new PagedQuery(2, 3));

        Assert.Equal([4, 5, 6], result.Items.Select(i => i.Id));
        Assert.Equal(7, result.TotalItems);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void The_last_page_can_be_partial()
    {
        var result = Numbered(7).ApplySort(null).ToPagedResult(new PagedQuery(3, 3));

        Assert.Equal([7], result.Items.Select(i => i.Id));
        Assert.Equal(7, result.TotalItems);
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void A_page_beyond_the_end_is_empty_but_still_reports_the_total()
    {
        var result = Numbered(7).ApplySort(null).ToPagedResult(new PagedQuery(9, 3));

        Assert.Empty(result.Items);
        Assert.Equal(7, result.TotalItems);
        Assert.Equal(9, result.Page);
    }

    private static IQueryable<ListItem> Numbered(int count) =>
        Enumerable.Range(1, count).Select(i => new ListItem { Id = i, Name = $"n{i}" }).Reverse().AsQueryable();
}
