using Learnix.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.State = EntityState.Modified;
            entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
            entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = DateTime.UtcNow;

            RestoreCascadedDependents(entry);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Rescues the dependents EF cascaded into Deleted when the principal was marked Deleted a moment
    /// ago. Turning the principal itself back into an UPDATE is not enough: a soft-deleted Course is
    /// loaded with its Sections, EF cascades those into Deleted, and nothing else puts them back — so
    /// the row that survives is a course whose content was destroyed, and Recover() hands back an empty
    /// shell. Whatever soft-delete means, it does not mean that.
    ///
    /// Only entries that are still Deleted are touched, and only those reachable from the principal, so
    /// an entity the handler deleted in its own right is left alone.
    /// </summary>
    private static void RestoreCascadedDependents(EntityEntry entry)
    {
        foreach (var navigation in entry.Navigations)
        {
            // Navigations declared on the dependent point back at principals — those were not cascaded.
            if (navigation.Metadata is not INavigation { IsOnDependent: false })
                continue;

            foreach (var dependent in Dependents(navigation))
            {
                var dependentEntry = entry.Context.Entry(dependent);

                if (dependentEntry.State != EntityState.Deleted)
                    continue;

                dependentEntry.State = EntityState.Unchanged;

                RestoreCascadedDependents(dependentEntry);
            }
        }
    }

    private static IEnumerable<object> Dependents(NavigationEntry navigation)
    {
        if (navigation is CollectionEntry collection)
            return collection.CurrentValue?.Cast<object>() ?? [];

        return navigation.CurrentValue is { } dependent ? [dependent] : [];
    }
}
