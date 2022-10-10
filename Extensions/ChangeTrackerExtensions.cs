using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EfVueMantle.Extensions;
//credit to: https://digitaldrummerj.me/ef-core-soft-deletes/
public static class ChangeTrackerExtensions
{
    public static void SetAuditProperties<TKey>(this ChangeTracker changeTracker)
        where TKey : IEquatable<TKey>
    {
        changeTracker.DetectChanges();
        IEnumerable<EntityEntry> entities =
            changeTracker
                .Entries()
                .Where(t => t.Entity is ISoftDelete<TKey> && t.State == EntityState.Deleted);
        foreach (EntityEntry entry in entities)
        {
            ISoftDelete<TKey> entity = (ISoftDelete<TKey>)entry.Entity;
            entity.Deleted = true;
            //entity.DeletedByUserId = userId;
            entity.DeletedDateTime = DateTime.Now;
            entry.State = EntityState.Modified;
        }
    }
}
