using MiniWms.Domain.Common;

namespace MiniWms.Domain.Locations;

/// <summary>The single warehouse (LOC-1.1). No endpoint creates or deletes warehouses.</summary>
public class Warehouse : AuditableEntity
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;

    private Warehouse()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public static Warehouse Create(string? code, string? name) => new()
    {
        Code = Guard.RequiredText(code, "code", "Code", CodeMaxLength),
        Name = Guard.RequiredText(name, "name", "Name", NameMaxLength),
    };

    /// <summary>The code never changes after creation (LOC-2.1).</summary>
    public void Rename(string? name) => Name = Guard.RequiredText(name, "name", "Name", NameMaxLength);
}
