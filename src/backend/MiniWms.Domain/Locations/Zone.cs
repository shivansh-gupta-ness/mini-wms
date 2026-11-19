using MiniWms.Domain.Common;

namespace MiniWms.Domain.Locations;

public class Zone : AuditableEntity
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;

    private Zone()
    {
    }

    public int WarehouseId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ZoneType Type { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Zone Create(int warehouseId, string? code, string? name, ZoneType type) => new()
    {
        WarehouseId = warehouseId,
        Code = Guard.RequiredText(code, "code", "Code", CodeMaxLength),
        Name = Guard.RequiredText(name, "name", "Name", NameMaxLength),
        Type = EnsureValidType(type),
    };

    /// <summary>The code never changes after creation (LOC-2.1).</summary>
    public void Rename(string? name) => Name = Guard.RequiredText(name, "name", "Name", NameMaxLength);

    /// <param name="hasBins">Whether the zone has any bin, active or not (LOC-2.1).</param>
    public void ChangeType(ZoneType type, bool hasBins)
    {
        EnsureValidType(type);
        if (type == Type)
        {
            return;
        }

        if (hasBins)
        {
            throw new DomainException("The type of a zone cannot be changed once it has bins.");
        }

        Type = type;
    }

    /// <param name="hasActiveBins">Whether the zone has any Active bin (LOC-2.4).</param>
    public void Deactivate(bool hasActiveBins)
    {
        if (hasActiveBins)
        {
            throw new DomainException("A zone with active bins cannot be deactivated.");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;

    private static ZoneType EnsureValidType(ZoneType type) =>
        Enum.IsDefined(type) ? type : throw new ValidationFailedException("type", "Zone type must be Receiving, Storage or Shipping.");
}
