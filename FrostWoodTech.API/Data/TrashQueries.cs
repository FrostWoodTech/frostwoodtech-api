using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Data;

internal static class TrashQueries
{
    // IgnoreQueryFilters is query-wide, so one call at the root also covers joined tables.
    public static IQueryable<T> Trashed<T>(this DbSet<T> set) where T : AuditableEntity =>
        set.IgnoreQueryFilters().Where(e => e.IsDeleted);

    public static Task<T?> FindTrashedAsync<T>(this DbSet<T> set, Guid id, CancellationToken cancellationToken)
        where T : AuditableEntity =>
        set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, cancellationToken);
}
