using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Locations;
using MiniWms.Domain.Locations;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Infrastructure.Locations;

/// <summary>EF Core implementation of the Locations read side: no tracking, projections straight to DTOs.</summary>
internal sealed class LocationQueries(AppDbContext context) : ILocationQueries
{
    private const string LikeEscape = "\\";

    // Zone type is stored as its name; sorting by the column would be alphabetical, the contract wants enum order.
    private static readonly Expression<Func<Zone, int>> ZoneTypeOrder =
        z => z.Type == ZoneType.Receiving ? 0 : z.Type == ZoneType.Storage ? 1 : 2;

    public Task<WarehouseDto?> GetWarehouseAsync(CancellationToken cancellationToken) =>
        context.Warehouses.AsNoTracking()
            .OrderBy(w => w.Id)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.IsActive, w.Version))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ZoneDto>> ListZonesAsync(
        PagedQuery paging, SortSpec<Zone>? sort, bool? isActive, ZoneType? type, string? q, CancellationToken cancellationToken)
    {
        var zones = context.Zones.AsNoTracking();
        if (isActive is { } active)
        {
            zones = zones.Where(z => z.IsActive == active);
        }

        if (type is { } zoneType)
        {
            zones = zones.Where(z => z.Type == zoneType);
        }

        if (q is not null)
        {
            var pattern = LikePattern(q);
            zones = zones.Where(z =>
                EF.Functions.Like(z.Code, pattern, LikeEscape) || EF.Functions.Like(z.Name, pattern, LikeEscape));
        }

        // Name has no column collation (BINARY); order it case-insensitively here, because ApplySort must stay free of EF Core.
        var sorted = string.Equals(sort?.Field, "type", StringComparison.OrdinalIgnoreCase)
            ? (sort!.Descending ? zones.OrderByDescending(ZoneTypeOrder) : zones.OrderBy(ZoneTypeOrder)).ThenBy(z => z.Id)
            : string.Equals(sort?.Field, "name", StringComparison.OrdinalIgnoreCase)
                ? (sort!.Descending
                    ? zones.OrderByDescending(z => EF.Functions.Collate(z.Name, "NOCASE"))
                    : zones.OrderBy(z => EF.Functions.Collate(z.Name, "NOCASE"))).ThenBy(z => z.Id)
                : zones.ApplySort(sort);

        var page = await sorted
            .Select(z => new ZoneRow(
                z.Id, z.WarehouseId, z.Code, z.Name, z.Type, z.IsActive, context.Bins.Count(b => b.ZoneId == z.Id), z.Version))
            .ToPagedResultAsync(paging, cancellationToken);

        return new PagedResult<ZoneDto>([.. page.Items.Select(r => r.ToDto())], page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<ZoneDto?> GetZoneAsync(int id, CancellationToken cancellationToken)
    {
        var row = await context.Zones.AsNoTracking()
            .Where(z => z.Id == id)
            .Select(z => new ZoneRow(
                z.Id, z.WarehouseId, z.Code, z.Name, z.Type, z.IsActive, context.Bins.Count(b => b.ZoneId == z.Id), z.Version))
            .FirstOrDefaultAsync(cancellationToken);
        return row?.ToDto();
    }

    public async Task<PagedResult<BinDto>> ListBinsAsync(
        PagedQuery paging, SortSpec<BinDto>? sort, bool? isActive, int? zoneId, string? q, CancellationToken cancellationToken)
    {
        var bins = BinPairs();
        if (isActive is { } active)
        {
            bins = bins.Where(p => p.Bin.IsActive == active);
        }

        if (zoneId is { } zone)
        {
            bins = bins.Where(p => p.Bin.ZoneId == zone);
        }

        if (q is not null)
        {
            var pattern = LikePattern(q);
            bins = bins.Where(p =>
                EF.Functions.Like(p.Bin.Code, pattern, LikeEscape) || EF.Functions.Like(p.Bin.Name, pattern, LikeEscape));
        }

        var descending = sort?.Descending ?? false;
        IOrderedQueryable<BinPair> sorted = sort?.Field.ToLowerInvariant() switch
        {
            "code" => (descending ? bins.OrderByDescending(p => p.Bin.Code) : bins.OrderBy(p => p.Bin.Code)).ThenBy(p => p.Bin.Id),
            "name" => (descending ? bins.OrderByDescending(p => EF.Functions.Collate(p.Bin.Name, "NOCASE")) : bins.OrderBy(p => EF.Functions.Collate(p.Bin.Name, "NOCASE"))).ThenBy(p => p.Bin.Id),
            "zone" => (descending ? bins.OrderByDescending(p => p.Zone.Code) : bins.OrderBy(p => p.Zone.Code)).ThenBy(p => p.Bin.Id),
            "isactive" => (descending ? bins.OrderByDescending(p => p.Bin.IsActive) : bins.OrderBy(p => p.Bin.IsActive)).ThenBy(p => p.Bin.Id),
            _ => bins.OrderBy(p => p.Bin.Id),
        };

        var page = await sorted
            .Select(p => new BinRow(p.Bin.Id, p.Bin.ZoneId, p.Zone.Code, p.Bin.Code, p.Bin.Name, p.Bin.IsActive, p.Bin.Version))
            .ToPagedResultAsync(paging, cancellationToken);

        return new PagedResult<BinDto>([.. page.Items.Select(r => r.ToDto())], page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<BinDto?> GetBinAsync(int id, CancellationToken cancellationToken)
    {
        var row = await BinPairs()
            .Where(p => p.Bin.Id == id)
            .Select(p => new BinRow(p.Bin.Id, p.Bin.ZoneId, p.Zone.Code, p.Bin.Code, p.Bin.Name, p.Bin.IsActive, p.Bin.Version))
            .FirstOrDefaultAsync(cancellationToken);
        return row?.ToDto();
    }

    public async Task<IReadOnlyList<ZoneNodeDto>> GetTreeAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var zones = context.Zones.AsNoTracking();
        var bins = BinPairs();
        if (isActive is { } active)
        {
            zones = zones.Where(z => z.IsActive == active);
            bins = bins.Where(p => p.Bin.IsActive == active);
        }

        var zoneRows = await zones
            .OrderBy(z => z.Code).ThenBy(z => z.Id)
            .Select(z => new ZoneRow(
                z.Id, z.WarehouseId, z.Code, z.Name, z.Type, z.IsActive, context.Bins.Count(b => b.ZoneId == z.Id), z.Version))
            .ToListAsync(cancellationToken);
        var binRows = await bins
            .OrderBy(p => p.Bin.Code).ThenBy(p => p.Bin.Id)
            .Select(p => new BinRow(p.Bin.Id, p.Bin.ZoneId, p.Zone.Code, p.Bin.Code, p.Bin.Name, p.Bin.IsActive, p.Bin.Version))
            .ToListAsync(cancellationToken);

        var binsByZone = binRows.ToLookup(b => b.ZoneId, b => b.ToDto());
        return [.. zoneRows.Select(z => z.ToNode([.. binsByZone[z.Id]]))];
    }

    private IQueryable<BinPair> BinPairs() =>
        from b in context.Bins.AsNoTracking()
        join z in context.Zones.AsNoTracking() on b.ZoneId equals z.Id
        select new BinPair { Bin = b, Zone = z };

    /// <summary>Contains-pattern for LIKE with <c>%</c>, <c>_</c> and the escape character taken literally.</summary>
    private static string LikePattern(string q) =>
        "%" + q.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_") + "%";

    private sealed class BinPair
    {
        public required Bin Bin { get; init; }

        public required Zone Zone { get; init; }
    }

    private sealed record BinRow(int Id, int ZoneId, string ZoneCode, string Code, string Name, bool IsActive, int Version)
    {
        public BinDto ToDto() => new(Id, ZoneId, ZoneCode, Code, Name, ZoneCode + "/" + Code, IsActive, Version);
    }

    private sealed record ZoneRow(int Id, int WarehouseId, string Code, string Name, ZoneType Type, bool IsActive, int BinCount, int Version)
    {
        public ZoneDto ToDto() => new(Id, WarehouseId, Code, Name, Type.ToString(), IsActive, BinCount, Version);

        public ZoneNodeDto ToNode(IReadOnlyList<BinDto> bins) =>
            new(Id, WarehouseId, Code, Name, Type.ToString(), IsActive, BinCount, Version, bins);
    }
}
