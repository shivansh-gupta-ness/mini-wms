using System.Linq.Expressions;

namespace MiniWms.Application.Common.Sorting;

/// <summary>A parsed sort request: one allowed field and a direction.</summary>
public sealed record SortSpec<TEntity>(string Field, bool Descending, Expression<Func<TEntity, object?>> Selector);

/// <summary>Maps the sort field names a module allows (case-insensitive) to the property each sorts by.</summary>
public sealed class SortFields<TEntity>
{
    private readonly Dictionary<string, Expression<Func<TEntity, object?>>> _fields =
        new(StringComparer.OrdinalIgnoreCase);

    public SortFields<TEntity> Add(string name, Expression<Func<TEntity, object?>> selector)
    {
        _fields[name] = selector;
        return this;
    }

    public IReadOnlyCollection<string> Names => _fields.Keys;

    public bool TryGet(string name, out Expression<Func<TEntity, object?>> selector) =>
        _fields.TryGetValue(name, out selector!);
}
