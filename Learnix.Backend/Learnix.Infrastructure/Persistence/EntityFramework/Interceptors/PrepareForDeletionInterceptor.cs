using Learnix.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Gives every entity that is about to be hard-deleted a chance to release the external resources it
/// owns (blobs, mostly) by raising domain events, so that deleting a row never orphans a file in Azure
/// Storage. Doing this here rather than in the handlers means a new delete handler cannot forget to.
///
/// Ordering contract, both halves of it, owned by PersistenceModule.AddPersistence and locked in by
/// PrepareForDeletionInterceptorTests:
///
/// - AFTER SoftDeleteInterceptor, which rescues the dependents EF cascaded into Deleted behind a
///   soft-deleted principal. Run first and we would see a soft-deleted Course's lessons still sitting in
///   Deleted and release videos out from under a course that is about to be recovered.
/// - BEFORE DomainEventsInterceptor, which collects DomainEvents off the tracked entities and
///   dispatches them. Events raised after that sweep would sit on the entity unnoticed and be lost.
///
/// ChangeTracker, not the handler's own argument, is the source of truth here: it also holds the
/// dependents EF cascaded into Deleted on its own — delete a Section and its Lessons follow, releasing
/// their videos, which the aggregate never did on that path. The catch is that EF only cascades to
/// children it has loaded, so this stays correct only as long as delete paths keep loading the entities
/// that own blobs — TD-011.
/// </summary>
public sealed class PrepareForDeletionInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            // Redundant while SoftDeleteInterceptor runs first — it has already turned these into
            // Modified — but it states the invariant locally: a row that survives the delete and can be
            // brought back (Course.Recover, AdminRecoverUser) keeps its blobs, or recovery would hand
            // back an entity whose image or video no longer exists.
            if (entry.Entity is ISoftDeletable)
                continue;

            // Idempotent by contract (BaseEntity.PrepareForDeletion), so an aggregate root that already
            // prepared its child explicitly — Course.RemoveLesson does — does not release it twice.
            entry.Entity.PrepareForDeletion();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
