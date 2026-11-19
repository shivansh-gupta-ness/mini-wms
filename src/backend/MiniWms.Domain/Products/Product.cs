using MiniWms.Domain.Common;

namespace MiniWms.Domain.Products;

public class Product : AuditableEntity
{
    public const int SkuMaxLength = 40;
    public const int NameMaxLength = 150;
    public const int UnitOfMeasureMaxLength = 20;

    private Product()
    {
    }

    /// <summary>Immutable after creation (PRD-2.1); unique case-insensitively.</summary>
    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string UnitOfMeasure { get; private set; } = string.Empty;

    public int ReorderLevel { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Product Create(string? sku, string? name, string? unitOfMeasure, int reorderLevel) => new()
    {
        Sku = Guard.RequiredText(sku, "SKU", SkuMaxLength),
        Name = Guard.RequiredText(name, "Name", NameMaxLength),
        UnitOfMeasure = Guard.RequiredText(unitOfMeasure, "Unit of measure", UnitOfMeasureMaxLength),
        ReorderLevel = EnsureReorderLevel(reorderLevel),
    };

    /// <summary>Changes everything except the SKU (PRD-2.1).</summary>
    public void Update(string? name, string? unitOfMeasure, int reorderLevel)
    {
        var newName = Guard.RequiredText(name, "Name", NameMaxLength);
        var newUnit = Guard.RequiredText(unitOfMeasure, "Unit of measure", UnitOfMeasureMaxLength);
        var newLevel = EnsureReorderLevel(reorderLevel);

        Name = newName;
        UnitOfMeasure = newUnit;
        ReorderLevel = newLevel;
    }

    /// <param name="totalOnHand">Total quantity of this product across all bins (PRD-2.2).</param>
    public void Deactivate(int totalOnHand)
    {
        if (totalOnHand > 0)
        {
            throw new DomainException(
                $"The product cannot be deactivated while it has stock ({totalOnHand} on hand).");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;

    private static int EnsureReorderLevel(int reorderLevel) =>
        reorderLevel >= 0 ? reorderLevel : throw new DomainException("Reorder level must be 0 or more.");
}
