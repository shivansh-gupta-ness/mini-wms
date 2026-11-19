using System.Globalization;
using System.Text.Json;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Locations;
using MiniWms.Domain.Common;

namespace MiniWms.Api.Locations;

internal sealed record RenameBody(string? Name, int? Version);

internal sealed record VersionBody(int? Version);

internal sealed record CreateZoneBody(string? Code, string? Name, string? Type);

internal sealed record UpdateZoneBody(string? Name, string? Type, int? Version);

internal sealed record CreateBinBody(int? ZoneId, string? Code, string? Name);

/// <summary>
/// Reads request bodies and query strings itself so every bad input becomes the standard field-level 400
/// (<see cref="ValidationFailedException"/>) instead of ASP.NET's bare 400, whatever the environment.
/// </summary>
internal static class LocationRequests
{
    public static async Task<T> ReadBodyAsync<T>(HttpRequest request, CancellationToken cancellationToken)
        where T : class
    {
        const string message = "The request body must be a JSON object with valid field values.";
        if (!request.HasJsonContentType())
        {
            throw new ValidationFailedException("body", message);
        }

        try
        {
            return await request.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new ValidationFailedException("body", message);
        }
        catch (JsonException)
        {
            throw new ValidationFailedException("body", message);
        }
    }

    public static ListZonesQuery ZoneListQuery(IQueryCollection query)
    {
        var errors = new Dictionary<string, string[]>();
        var page = Number(query, "page", PagedQuery.DefaultPage, errors);
        var pageSize = Number(query, "pageSize", PagedQuery.DefaultPageSize, errors);
        var isActive = Flag(query, "isActive", errors);
        ThrowIfAny(errors);

        return new ListZonesQuery(page, pageSize, Text(query, "sort"), isActive, Text(query, "type"), Text(query, "q"));
    }

    public static ListBinsQuery BinListQuery(IQueryCollection query)
    {
        var errors = new Dictionary<string, string[]>();
        var page = Number(query, "page", PagedQuery.DefaultPage, errors);
        var pageSize = Number(query, "pageSize", PagedQuery.DefaultPageSize, errors);
        var zoneId = NullableNumber(query, "zoneId", errors);
        var isActive = Flag(query, "isActive", errors);
        ThrowIfAny(errors);

        return new ListBinsQuery(page, pageSize, Text(query, "sort"), isActive, zoneId, Text(query, "q"));
    }

    public static GetLocationTreeQuery TreeQuery(IQueryCollection query)
    {
        var errors = new Dictionary<string, string[]>();
        var isActive = Flag(query, "isActive", errors);
        ThrowIfAny(errors);

        return new GetLocationTreeQuery(isActive);
    }

    private static string? Text(IQueryCollection query, string name) =>
        query.TryGetValue(name, out var value) ? value.ToString() : null;

    private static int Number(IQueryCollection query, string name, int fallback, Dictionary<string, string[]> errors) =>
        NullableNumber(query, name, errors) ?? fallback;

    private static int? NullableNumber(IQueryCollection query, string name, Dictionary<string, string[]> errors)
    {
        var raw = Text(query, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        errors[name] = [$"{Label(name)} must be a whole number."];
        return null;
    }

    private static bool? Flag(IQueryCollection query, string name, Dictionary<string, string[]> errors)
    {
        var raw = Text(query, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (bool.TryParse(raw.Trim(), out var value))
        {
            return value;
        }

        errors[name] = [$"{Label(name)} must be true or false."];
        return null;
    }

    private static string Label(string name) => name switch
    {
        "pageSize" => "Page size",
        "zoneId" => "Zone id",
        "isActive" => "Active filter",
        _ => char.ToUpperInvariant(name[0]) + name[1..],
    };

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }
    }
}
