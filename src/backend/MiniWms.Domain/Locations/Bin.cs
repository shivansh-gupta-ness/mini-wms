using MiniWms.Domain.Common;

namespace MiniWms.Domain.Locations;

public class Bin : AuditableEntity
{
    public const int CodeMaxLength = 30;
    public const int NameMaxLength = 100;

    private Bin()
    {
    }

    public int ZoneId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public static Bin Create(int zoneId, string? code, string? name) => new()
    {
        ZoneId = zoneId,
        Code = Guard.RequiredText(code, "code", "Code", CodeMaxLength),
        Name = Guard.RequiredText(name, "name", "Name", NameMaxLength),
    };

    /// <summary>The code never changes after creation (LOC-2.1).</summary>
    public void Rename(string? name) => Name = Guard.RequiredText(name, "name", "Name", NameMaxLength);

    /// <param name="totalOnHand">Total quantity held in this bin across all products (LOC-2.2).</param>
    public void Deactivate(int totalOnHand)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalOnHand);

        if (totalOnHand > 0)
        {
            throw new DomainException($"The bin cannot be deactivated while it holds stock ({totalOnHand} on hand).");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;
}
