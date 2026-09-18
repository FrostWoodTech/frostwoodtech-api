namespace FrostWoodTech.API.Entities.Common;

/// <summary>Timestamps are stamped by the DbContext; soft-deleted rows are hidden by a global filter.</summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
