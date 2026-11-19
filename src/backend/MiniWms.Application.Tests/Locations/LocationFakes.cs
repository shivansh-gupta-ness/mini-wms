using FluentValidation;
using MiniWms.Application.Common;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Locations;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Tests.Locations;

/// <summary>In-memory store; code-exists checks ignore case like the real NOCASE column.</summary>
internal sealed class FakeLocationStore : ILocationStore
{
    private int _nextId = 100;

    public Warehouse? Warehouse { get; set; }

    public List<Zone> Zones { get; } = [];

    public List<Bin> Bins { get; } = [];

    public List<(AuditableEntity Entity, int? ClientVersion)> Saves { get; } = [];

    public bool SaveCalled => Saves.Count > 0;

    public Task<Warehouse?> GetWarehouseAsync(CancellationToken cancellationToken) => Task.FromResult(Warehouse);

    public Task<Zone?> FindZoneAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));

    public Task<Bin?> FindBinAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Bins.FirstOrDefault(b => b.Id == id));

    public Task<bool> ZoneCodeExistsAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Zones.Any(z => string.Equals(z.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> BinCodeExistsAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Bins.Any(b => string.Equals(b.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ZoneHasBinsAsync(int zoneId, CancellationToken cancellationToken) =>
        Task.FromResult(Bins.Any(b => b.ZoneId == zoneId));

    public Task<bool> ZoneHasActiveBinsAsync(int zoneId, CancellationToken cancellationToken) =>
        Task.FromResult(Bins.Any(b => b.ZoneId == zoneId && b.IsActive));

    public void Add(Zone zone)
    {
        zone.Id = _nextId++;
        zone.Version = 1;
        Zones.Add(zone);
    }

    public void Add(Bin bin)
    {
        bin.Id = _nextId++;
        bin.Version = 1;
        Bins.Add(bin);
    }

    public Task SaveAsync(AuditableEntity entity, int? clientVersion, CancellationToken cancellationToken)
    {
        Saves.Add((entity, clientVersion));
        return Task.CompletedTask;
    }

    public Zone SeedZone(string code, ZoneType type = ZoneType.Storage, bool active = true)
    {
        var zone = Zone.Create(Warehouse?.Id ?? 1, code, code + " zone", type);
        if (!active)
        {
            zone.Deactivate(false);
        }

        Add(zone);
        return zone;
    }

    public Bin SeedBin(Zone zone, string code, bool active = true)
    {
        var bin = Bin.Create(zone.Id, code, code + " bin");
        if (!active)
        {
            bin.Deactivate(0);
        }

        Add(bin);
        return bin;
    }
}

internal sealed class FakeStockQuery : IStockQuery
{
    public Dictionary<int, int> BinStock { get; } = [];

    public int BinCalls { get; private set; }

    public Task<int> BinOnHandAsync(int binId, CancellationToken cancellationToken)
    {
        BinCalls++;
        return Task.FromResult(BinStock.GetValueOrDefault(binId));
    }

    public Task<int> ProductOnHandAsync(int productId, CancellationToken cancellationToken) => Task.FromResult(0);
}

internal sealed class FakeLocationQueries(FakeLocationStore store) : ILocationQueries
{
    public record ZoneListCall(PagedQuery Paging, SortSpec<Zone>? Sort, bool? IsActive, ZoneType? Type, string? Q);

    public record BinListCall(PagedQuery Paging, SortSpec<BinDto>? Sort, bool? IsActive, int? ZoneId, string? Q);

    public ZoneListCall? ZoneList { get; private set; }

    public BinListCall? BinList { get; private set; }

    public Task<WarehouseDto?> GetWarehouseAsync(CancellationToken cancellationToken)
    {
        var w = store.Warehouse;
        return Task.FromResult(w is null ? null : new WarehouseDto(w.Id, w.Code, w.Name, w.IsActive, w.Version));
    }

    public Task<PagedResult<ZoneDto>> ListZonesAsync(
        PagedQuery paging, SortSpec<Zone>? sort, bool? isActive, ZoneType? type, string? q, CancellationToken cancellationToken)
    {
        ZoneList = new ZoneListCall(paging, sort, isActive, type, q);
        return Task.FromResult(new PagedResult<ZoneDto>([], paging.Page, paging.PageSize, 0));
    }

    public Task<ZoneDto?> GetZoneAsync(int id, CancellationToken cancellationToken)
    {
        var z = store.Zones.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(z is null
            ? null
            : new ZoneDto(z.Id, z.WarehouseId, z.Code, z.Name, z.Type.ToString(), z.IsActive,
                store.Bins.Count(b => b.ZoneId == z.Id), z.Version));
    }

    public Task<PagedResult<BinDto>> ListBinsAsync(
        PagedQuery paging, SortSpec<BinDto>? sort, bool? isActive, int? zoneId, string? q, CancellationToken cancellationToken)
    {
        BinList = new BinListCall(paging, sort, isActive, zoneId, q);
        return Task.FromResult(new PagedResult<BinDto>([], paging.Page, paging.PageSize, 0));
    }

    public Task<BinDto?> GetBinAsync(int id, CancellationToken cancellationToken)
    {
        var b = store.Bins.FirstOrDefault(x => x.Id == id);
        if (b is null)
        {
            return Task.FromResult<BinDto?>(null);
        }

        var zone = store.Zones.First(z => z.Id == b.ZoneId);
        return Task.FromResult<BinDto?>(
            new BinDto(b.Id, b.ZoneId, zone.Code, b.Code, b.Name, $"{zone.Code}/{b.Code}", b.IsActive, b.Version));
    }

    public Task<IReadOnlyList<ZoneNodeDto>> GetTreeAsync(bool? isActive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ZoneNodeDto>>([]);
}

/// <summary>The failing field names of a rejected request, whichever exception type carries them.</summary>
internal static class Failures
{
    public static async Task<IReadOnlyCollection<string>> FieldsAsync(Func<Task> act)
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(act);
        return ex switch
        {
            ValidationException fluent => fluent.Errors.Select(e => Camel(e.PropertyName)).Distinct().ToList(),
            ValidationFailedException domain => domain.Errors.Keys.Select(Camel).ToList(),
            _ => throw new Xunit.Sdk.XunitException($"Expected a validation failure but got {ex.GetType().Name}: {ex.Message}"),
        };
    }

    private static string Camel(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}

internal static class Seed
{
    public static (FakeLocationStore Store, FakeLocationQueries Queries) WarehouseWithVersion3()
    {
        var store = new FakeLocationStore { Warehouse = MiniWms.Domain.Locations.Warehouse.Create("MAIN", "Main warehouse") };
        store.Warehouse.Id = 1;
        store.Warehouse.Version = 3;
        return (store, new FakeLocationQueries(store));
    }
}
