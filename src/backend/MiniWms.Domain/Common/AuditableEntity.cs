namespace MiniWms.Domain.Common;

/// <summary>
/// Base for every mutable table (docs/specs/data-model.md). The audit fields and
/// <see cref="Version"/> are set centrally by the persistence layer, never by use cases.
/// All timestamps are UTC.
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    /// <summary>Concurrency token: starts at 1 and increases by one on every update.</summary>
    public int Version { get; set; }
}
