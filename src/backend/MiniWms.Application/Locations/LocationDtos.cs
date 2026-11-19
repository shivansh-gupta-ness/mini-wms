namespace MiniWms.Application.Locations;

public record WarehouseDto(int Id, string Code, string Name, bool IsActive, int Version);

/// <param name="Type">The zone type word: Receiving, Storage or Shipping.</param>
/// <param name="BinCount">All bins of the zone, including inactive ones.</param>
public record ZoneDto(int Id, int WarehouseId, string Code, string Name, string Type, bool IsActive, int BinCount, int Version);

/// <param name="Path">Display path <c>ZONE/BIN</c>.</param>
public record BinDto(int Id, int ZoneId, string ZoneCode, string Code, string Name, string Path, bool IsActive, int Version);

/// <summary>A zone with its bins, one element of the location tree (the tree is a plain array of these).</summary>
public record ZoneNodeDto(
    int Id,
    int WarehouseId,
    string Code,
    string Name,
    string Type,
    bool IsActive,
    int BinCount,
    int Version,
    IReadOnlyList<BinDto> Bins);
