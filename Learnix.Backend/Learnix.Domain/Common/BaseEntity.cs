namespace Learnix.Domain.Common;

public abstract class BaseEntity : IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();

#pragma warning disable S1144
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
#pragma warning restore S1144

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    private bool _preparedForDeletion;

    /// <summary>
    /// Releases external resources (blobs, etc.) by raising domain events, right before the entity
    /// leaves the database for good. Invoked automatically by PrepareForDeletionInterceptor for every
    /// entity that EF Core is about to hard-delete, so handlers never have to remember to call it —
    /// an aggregate root may still call it explicitly on a child it is removing.
    ///
    /// Idempotent: the resources are released once no matter how many callers ask, so the explicit
    /// aggregate-level call and the interceptor sweep cannot enqueue the same blob deletion twice.
    ///
    /// Not invoked for ISoftDeletable entities: their row survives and may be recovered, so their
    /// external resources have to survive with it. Override <see cref="OnPreparingForDeletion"/>.
    /// </summary>
    public void PrepareForDeletion()
    {
        if (_preparedForDeletion)
            return;

        _preparedForDeletion = true;

        OnPreparingForDeletion();
    }

    /// <summary>
    /// Override in subclasses that own external resources. Runs at most once per entity instance.
    /// </summary>
    protected virtual void OnPreparingForDeletion() { }
}
