using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <summary>Read side of Locations: DTOs straight from the database. Implemented in Infrastructure.</summary>
public interface ILocationQueries
{
    Task<WarehouseDto?> GetWarehouseAsync(CancellationToken cancellationToken);

    /// <param name="q">Already trimmed; null for no search. Matches part of code or name, ASCII case-insensitive.</param>
    Task<PagedResult<ZoneDto>> ListZonesAsync(
        PagedQuery paging, SortSpec<Zone>? sort, bool? isActive, ZoneType? type, string? q, CancellationToken cancellationToken);

    Task<ZoneDto?> GetZoneAsync(int id, CancellationToken cancellationToken);

    /// <param name="sort">Sorts the projected <see cref="BinDto"/> (<c>zone</c> is the zone code).</param>
    Task<PagedResult<BinDto>> ListBinsAsync(
        PagedQuery paging, SortSpec<BinDto>? sort, bool? isActive, int? zoneId, string? q, CancellationToken cancellationToken);

    Task<BinDto?> GetBinAsync(int id, CancellationToken cancellationToken);

    /// <summary>Every zone with its bins; <paramref name="isActive"/> filters zones and bins independently.</summary>
    Task<IReadOnlyList<ZoneNodeDto>> GetTreeAsync(bool? isActive, CancellationToken cancellationToken);
}
