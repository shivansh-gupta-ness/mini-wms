namespace MiniWms.Application.Common.Sorting;

/// <summary>Parses <c>sort=field</c> or <c>sort=-field</c> against a module's allow-list. One field only.</summary>
public static class SortParser
{
    /// <summary>
    /// Returns null when <paramref name="sort"/> is omitted or blank (no default order). Otherwise returns the spec,
    /// or sets <paramref name="error"/> when the field is unknown, more than one field is given or the name is empty.
    /// </summary>
    public static SortSpec<TEntity>? Parse<TEntity>(string? sort, SortFields<TEntity> allowed, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sort))
        {
            return null;
        }

        var text = sort.Trim();
        var descending = text.StartsWith('-');
        var name = (descending ? text[1..] : text).Trim();

        if (name.Contains(','))
        {
            error = "Sort by one field only.";
            return null;
        }

        if (!allowed.TryGet(name, out var selector))
        {
            error = $"Sort field must be one of: {string.Join(", ", allowed.Names.Order(StringComparer.OrdinalIgnoreCase))}.";
            return null;
        }

        var canonical = allowed.Names.First(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        return new SortSpec<TEntity>(canonical, descending, selector);
    }
}
