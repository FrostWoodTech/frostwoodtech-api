namespace FrostWoodTech.API.Entities.Common;

/// <summary>Timestamps are stamped by the DbContext; soft-deleted rows are hidden by a global filter.</summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    /// <summary>Stamped by the DbContext when IsDeleted flips true, cleared on restore.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Set by the service: the pooled DbContext can't take a scoped CurrentUser.</summary>
    public Guid? DeletedBy { get; set; }
}
